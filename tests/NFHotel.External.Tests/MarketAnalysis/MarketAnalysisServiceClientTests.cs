using System.Net;
using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.External.Config;
using NFHotel.External.MarketAnalysis;

namespace NFHotel.External.Tests.MarketAnalysis;

/// <summary>
/// The client of the market analysis service (MIL-026) against <see cref="FakeMarketAnalysisService"/>, a fake that follows the
/// three run paths of the contract: start, state and result.
/// </summary>
public sealed class MarketAnalysisServiceClientTests
{
    private const string BaseUrl = "http://market-analysis-api:8000";
    private const string Key = "secret-key-that-must-not-be-logged";

    private static readonly MarketBookingDto Booking =
        new(7, "NF Hotel", true, 30, new DateOnly(2019, 1, 30), new DateOnly(2019, 3, 1), 1, 2, 2, 0, 0, "BB", "Portugal", "Direct", false, 0, "C", 0, "No Deposit", "Contract", 0, 1, 94.50m);

    private static readonly MarketAnalysisRunRequest Request = new(120, [Booking]);

    #region Request

    /// <summary>Positive: a run is started on the run path with insights on, the room count and the bookings in snake_case.</summary>
    [Fact]
    public async Task AnalyzeAsync_Bookings_StartsTheRunOfTheContract()
    {
        // Arrange
        var service = new FakeMarketAnalysisService();

        // Act
        var result = await CreateClient(service).AnalyzeAsync(Request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("POST /api/v1/market-analysis/runs?insights=true", service.Requests[0]);
        using var sent = JsonDocument.Parse(service.StartedBody!);
        Assert.Equal(120, sent.RootElement.GetProperty("room_count").GetInt32());
        Assert.False(sent.RootElement.TryGetProperty("period", out _));
        var row = Assert.Single(sent.RootElement.GetProperty("bookings").EnumerateArray());
        Assert.Equal(
            (7, "NF Hotel", true, 30, "2019-01-30", "2019-03-01", 1, 2, "Portugal", 94.5m),
            (row.GetProperty("booking_id").GetInt32(), row.GetProperty("hotel").GetString(), row.GetProperty("is_canceled").GetBoolean(), row.GetProperty("lead_time").GetInt32(), row.GetProperty("booking_date").GetString(), row.GetProperty("arrival_date").GetString(), row.GetProperty("stays_in_weekend_nights").GetInt32(), row.GetProperty("stays_in_week_nights").GetInt32(), row.GetProperty("country").GetString(), row.GetProperty("price_per_night").GetDecimal()));
        Assert.Equal("No Deposit", row.GetProperty("deposit_type").GetString());
        Assert.Equal(("market-analysis-api", 8000), (service.LastAuthority.Split(':')[0], int.Parse(service.LastAuthority.Split(':')[1], System.Globalization.CultureInfo.InvariantCulture)));
    }

    /// <summary>Positive: a period with both dates is sent as <c>period</c>; with one date or none it is left out.</summary>
    [Theory]
    [InlineData("2019-03-01", "2019-03-31", true)]
    [InlineData("2019-03-01", null, false)]
    [InlineData(null, "2019-03-31", false)]
    [InlineData(null, null, false)]
    public async Task AnalyzeAsync_Period_IsSentOnlyWithBothDates(string? first, string? last, bool sent)
    {
        // Arrange
        var service = new FakeMarketAnalysisService();
        var request = Request with { From = first is null ? null : DateOnly.Parse(first, System.Globalization.CultureInfo.InvariantCulture), To = last is null ? null : DateOnly.Parse(last, System.Globalization.CultureInfo.InvariantCulture) };

        // Act
        await CreateClient(service).AnalyzeAsync(request, TestContext.Current.CancellationToken);

        // Assert
        using var body = JsonDocument.Parse(service.StartedBody!);
        Assert.Equal(sent, body.RootElement.TryGetProperty("period", out var period));
        if (sent)
        {
            Assert.Equal((first, last), (period.GetProperty("first_date").GetString(), period.GetProperty("last_date").GetString()));
        }
    }

    /// <summary>Boundary: a booking without market details leaves those fields out, so the service counts them as missing.</summary>
    [Fact]
    public async Task AnalyzeAsync_BookingWithoutDetails_LeavesTheUnknownFieldsOut()
    {
        // Arrange
        var service = new FakeMarketAnalysisService();
        var bare = new MarketBookingDto(1, "NF Hotel", false, null, null, new DateOnly(2019, 3, 1), 0, 1, null, null, null, null, "Denmark", null, null, null, null, null, null, null, null, null, null);

        // Act
        var result = await CreateClient(service).AnalyzeAsync(Request with { Bookings = [bare] }, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        using var sent = JsonDocument.Parse(service.StartedBody!);
        var names = sent.RootElement.GetProperty("bookings")[0].EnumerateObject().Select(property => property.Name).Order().ToArray();
        Assert.Equal(["arrival_date", "booking_id", "country", "hotel", "is_canceled", "stays_in_week_nights", "stays_in_weekend_nights"], names);
    }

    /// <summary>Positive: the API key is sent as X-API-Key on every call when it is set, and left out when it is empty.</summary>
    [Fact]
    public async Task AnalyzeAsync_ApiKey_IsSentOnEveryCallOnlyWhenSet()
    {
        // Arrange
        var with = new FakeMarketAnalysisService { RequiredKey = Key };
        var without = new FakeMarketAnalysisService();

        // Act
        var keyed = await CreateClient(with, ApiKeyOptions()).AnalyzeAsync(Request, TestContext.Current.CancellationToken);
        await CreateClient(without).AnalyzeAsync(Request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(keyed.IsSuccess);
        Assert.Equal(Key, with.LastKey);
        Assert.Null(without.LastKey);
    }

    #endregion

    #region Following the run

    /// <summary>Positive: the client follows the run by its id: the state until it is completed, then the result once.</summary>
    [Fact]
    public async Task AnalyzeAsync_RunQueuedThenRunning_FollowsTheStateThenReadsTheResult()
    {
        // Arrange
        var service = new FakeMarketAnalysisService();
        var time = new ManualTimeProvider();

        // Act
        var result = await CreateClient(service, time: time).AnalyzeAsync(Request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(
            ["POST /api/v1/market-analysis/runs?insights=true", "GET state", "GET state", "GET state", "GET result"],
            service.Requests.Select(Describe));
        Assert.All(service.Requests.Skip(1), request => Assert.Matches(@"^GET /api/v1/market-analysis/runs/[0-9a-f-]{36}(/result)?$", request));
        Assert.Equal([TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5)], time.Delays);
    }

    /// <summary>Boundary: a run that is completed at the first ask is read at once, without waiting.</summary>
    [Fact]
    public async Task AnalyzeAsync_RunCompletedAtOnce_DoesNotWait()
    {
        // Arrange
        var time = new ManualTimeProvider();

        // Act
        var result = await CreateClient(new FakeMarketAnalysisService { States = ["completed"] }, time: time).AnalyzeAsync(Request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(time.Delays);
    }

    /// <summary>Negative: a run that does not end within the deadline is given up as unavailable, asking at the poll interval.</summary>
    [Fact]
    public async Task AnalyzeAsync_RunNeverEnds_IsGivenUpAtTheDeadline()
    {
        // Arrange
        var service = new FakeMarketAnalysisService { States = ["running"] };
        var time = new ManualTimeProvider();
        var options = new MarketAnalysisServiceOptions { BaseUrl = BaseUrl, PollIntervalSeconds = 10, RunDeadlineSeconds = 60 };

        // Act
        var result = await CreateClient(service, options, time).AnalyzeAsync(Request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.Unavailable, Assert.Single(result.Errors).Code);
        Assert.Equal(6, time.Delays.Count);
        Assert.All(time.Delays, delay => Assert.Equal(TimeSpan.FromSeconds(10), delay));
        Assert.DoesNotContain(service.Requests, request => request.EndsWith("/result", StringComparison.Ordinal));
    }

    /// <summary>Negative: a run that ends as failed shows the sanitised error code of the service and reads no result.</summary>
    [Theory]
    [InlineData("INPUT_ERROR", "market_analysis_runner.run_failed:INPUT_ERROR")]
    [InlineData("bad code<script>", "market_analysis_runner.run_failed:badcodescript")]
    [InlineData(null, "market_analysis_runner.run_failed")]
    [InlineData("<>", "market_analysis_runner.run_failed")]
    public async Task AnalyzeAsync_RunFailed_ReturnsTheSanitisedErrorCode(string? errorCode, string expected)
    {
        // Arrange
        var service = new FakeMarketAnalysisService { States = ["running", "failed"], ErrorCode = errorCode };

        // Act
        var result = await CreateClient(service, time: new ManualTimeProvider()).AnalyzeAsync(Request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expected, Assert.Single(result.Errors).Code);
        Assert.DoesNotContain(service.Requests, request => request.EndsWith("/result", StringComparison.Ordinal));
    }

    /// <summary>Boundary: the error code of a failed run is cut to 64 characters.</summary>
    [Fact]
    public async Task AnalyzeAsync_RunFailedWithLongCode_CutsTheCode()
    {
        // Arrange
        var service = new FakeMarketAnalysisService { States = ["failed"], ErrorCode = new string('A', 200) };

        // Act
        var result = await CreateClient(service).AnalyzeAsync(Request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("market_analysis_runner.run_failed:" + new string('A', 64), Assert.Single(result.Errors).Code);
    }

    /// <summary>Positive: one lost answer while the run goes on is asked again, and the run is read to its end.</summary>
    [Fact]
    public async Task AnalyzeAsync_OneStateAskFails_AsksAgain()
    {
        // Arrange
        var service = new FakeMarketAnalysisService { ThrowOnStateRead = new HashSet<int> { 2 } };

        // Act
        var result = await CreateClient(service, time: new ManualTimeProvider()).AnalyzeAsync(Request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
    }

    /// <summary>Negative: three lost answers in a row end the follow-up as unavailable.</summary>
    [Fact]
    public async Task AnalyzeAsync_StateAsksFailThreeTimes_IsUnavailable()
    {
        // Arrange
        var service = new FakeMarketAnalysisService { ThrowOnStateRead = new HashSet<int> { 1, 2, 3 } };

        // Act
        var result = await CreateClient(service, time: new ManualTimeProvider()).AnalyzeAsync(Request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.Unavailable, Assert.Single(result.Errors).Code);
    }

    #endregion

    #region Failures

    /// <summary>Negative: each answer of the start has its own code (the table of the contract errors).</summary>
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "unauthorized", "market_analysis_runner.unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "forbidden", "market_analysis_runner.unauthorized")]
    [InlineData(HttpStatusCode.BadRequest, "invalid_request", "market_analysis_runner.input_rejected")]
    [InlineData(HttpStatusCode.UnprocessableEntity, "invalid_request", "market_analysis_runner.input_rejected")]
    [InlineData(HttpStatusCode.Conflict, "run_active", "market_analysis_runner.already_running")]
    [InlineData(HttpStatusCode.Conflict, "something_else", "market_analysis_runner.unexpected_response")]
    [InlineData(HttpStatusCode.InternalServerError, "internal", "market_analysis_runner.unavailable")]
    [InlineData(HttpStatusCode.BadGateway, "internal", "market_analysis_runner.unavailable")]
    [InlineData(HttpStatusCode.NotFound, "not_found", "market_analysis_runner.unexpected_response")]
    public async Task AnalyzeAsync_StartRefused_ReturnsTheMatchingCode(HttpStatusCode status, string problem, string code)
    {
        // Arrange
        var service = new FakeMarketAnalysisService { StartStatus = status, StartProblem = problem };

        // Act
        var result = await CreateClient(service).AnalyzeAsync(Request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(code, Assert.Single(result.Errors).Code);
        Assert.Single(service.Requests);
    }

    /// <summary>Negative: another run is active in the service, so the start answers 409 run_active and the code says so.</summary>
    [Fact]
    public async Task AnalyzeAsync_ServiceBusy_ReturnsAlreadyRunning()
    {
        // Act
        var result = await CreateClient(new FakeMarketAnalysisService { Busy = true }).AnalyzeAsync(Request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.AlreadyRunning, Assert.Single(result.Errors).Code);
    }

    /// <summary>Negative: a wrong key is a 401 from the fake, and the key is not in the log.</summary>
    [Fact]
    public async Task AnalyzeAsync_WrongKey_IsUnauthorizedAndTheKeyIsNotLogged()
    {
        // Arrange
        var service = new FakeMarketAnalysisService { RequiredKey = "the-right-key" };
        var log = new ListLogger();

        // Act
        var result = await CreateClient(service, ApiKeyOptions(), logger: log).AnalyzeAsync(Request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.Unauthorized, Assert.Single(result.Errors).Code);
        Assert.NotEmpty(log.Messages);
        Assert.DoesNotContain(log.Messages, message => message.Contains(Key, StringComparison.Ordinal) || message.Contains("the-right-key", StringComparison.Ordinal));
    }

    /// <summary>Negative: a 401 while the state is followed (the key was changed) is the unauthorized code too.</summary>
    [Fact]
    public async Task AnalyzeAsync_StateAnswers401_IsUnauthorized()
    {
        // Act
        var result = await CreateClient(new FakeMarketAnalysisService { StateStatus = HttpStatusCode.Unauthorized }, time: new ManualTimeProvider()).AnalyzeAsync(Request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.Unauthorized, Assert.Single(result.Errors).Code);
    }

    /// <summary>Negative: the service forgot the run (404 on the state, after a restart) is the unexpected response code.</summary>
    [Fact]
    public async Task AnalyzeAsync_StateAnswers404_IsUnexpected()
    {
        // Act
        var result = await CreateClient(new FakeMarketAnalysisService { StateStatus = HttpStatusCode.NotFound }, time: new ManualTimeProvider()).AnalyzeAsync(Request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.UnexpectedResponse, Assert.Single(result.Errors).Code);
    }

    /// <summary>Negative: a service that cannot be reached is the unavailable code and nothing is thrown.</summary>
    [Fact]
    public async Task AnalyzeAsync_ServiceDown_ReturnsTheUnavailableCode()
    {
        // Act
        var result = await CreateClient(new FakeMarketAnalysisService { Throw = true }).AnalyzeAsync(Request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.Unavailable, Assert.Single(result.Errors).Code);
    }

    /// <summary>Negative: a call that times out (a cancellation that the caller did not ask for) is the unavailable code.</summary>
    [Fact]
    public async Task AnalyzeAsync_CallTimesOut_ReturnsTheUnavailableCode()
    {
        // Act
        var result = await CreateClient(new FakeMarketAnalysisService { Timeout = true }).AnalyzeAsync(Request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.Unavailable, Assert.Single(result.Errors).Code);
    }

    /// <summary>Negative: unreadable JSON from the start, the state or the result is the unexpected response code.</summary>
    [Theory]
    [InlineData("start", "<html>")]
    [InlineData("start", "{\"state\":\"queued\"}")]
    [InlineData("start", "[]")]
    [InlineData("state", "<html>")]
    [InlineData("state", "{\"run_id\":\"5b3f0e1a-0000-4000-8000-0000000000a1\",\"state\":\"weird\"}")]
    [InlineData("state", "{\"run_id\":\"5b3f0e1a-0000-4000-8000-0000000000a1\"}")]
    [InlineData("result", "<html>")]
    [InlineData("result", "[]")]
    [InlineData("result", "{\"status\":\"completed\"}")]
    [InlineData("result", "{\"schema_version\":\"1.1\",\"status\":\"failed\"}")]
    public async Task AnalyzeAsync_AnswerIsNotUsable_ReturnsTheUnexpectedCode(string call, string body)
    {
        // Arrange
        var service = call switch
        {
            "start" => new FakeMarketAnalysisService { StartBody = body },
            "state" => new FakeMarketAnalysisService { StateBody = body },
            _ => body == "<html>" ? new FakeMarketAnalysisService { ResultIsNotJson = true } : new FakeMarketAnalysisService { Result = body },
        };

        // Act
        var result = await CreateClient(service, time: new ManualTimeProvider()).AnalyzeAsync(Request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.UnexpectedResponse, Assert.Single(result.Errors).Code);
    }

    /// <summary>Negative: without an address nothing is sent and the not configured code is returned.</summary>
    [Fact]
    public async Task AnalyzeAsync_NotConfigured_SendsNothing()
    {
        // Arrange
        var service = new FakeMarketAnalysisService();

        // Act
        var result = await CreateClient(service, new MarketAnalysisServiceOptions()).AnalyzeAsync(Request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.NotConfigured, Assert.Single(result.Errors).Code);
        Assert.Empty(service.Requests);
    }

    /// <summary>Negative: a caller that cancels gets the cancellation and not a code.</summary>
    [Fact]
    public async Task AnalyzeAsync_CallerCancels_ThrowsTheCancellation()
    {
        // Arrange
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        // Act and assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateClient(new FakeMarketAnalysisService()).AnalyzeAsync(Request, cancelled.Token));
    }

    #endregion

    #region Result

    /// <summary>Positive: the sample result of the service is read through the three calls into the nine sections.</summary>
    [Fact]
    public async Task AnalyzeAsync_SampleResult_ReadsTheNineAnalyses()
    {
        // Arrange
        var service = new FakeMarketAnalysisService { Result = SampleJson() };

        // Act
        var result = await CreateClient(service, time: new ManualTimeProvider()).AnalyzeAsync(Request, TestContext.Current.CancellationToken);

        // Assert
        var dto = result.Value;
        Assert.Equal(1, dto.SchemaVersion);
        Assert.Equal(
            ["lead_time", "holidays", "seasonality", "cancellations", "room_value", "guest_mix", "occupancy", "occupancy_on_holidays", "correlations"],
            dto.Content!.Sections.Select(section => section.Id));
        Assert.Equal(["Bookings analysed", "Bookings with a price of 0", "Duplicate booking ids"], dto.Content.Figures.Select(figure => figure.Label));
    }

    #endregion

    #region Registration

    /// <summary>Positive: the registration reads the section and the client uses the address and the key.</summary>
    [Fact]
    public async Task AddMarketAnalysisService_Configured_UsesTheAddressAndTheKey()
    {
        // Arrange
        var service = new FakeMarketAnalysisService { RequiredKey = Key };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MarketAnalysisService:BaseUrl"] = BaseUrl,
                ["MarketAnalysisService:ApiKey"] = Key,
                ["MarketAnalysisService:PollIntervalSeconds"] = "1",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMarketAnalysisService(configuration);
        services.AddHttpClient<IMarketAnalysisServiceClient, MarketAnalysisServiceClient>().ConfigurePrimaryHttpMessageHandler(() => service);
        services.AddSingleton<TimeProvider>(new ManualTimeProvider());
        using var provider = services.BuildServiceProvider();

        // Act
        var result = await provider.GetRequiredService<IMarketAnalysisServiceClient>().AnalyzeAsync(Request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("market-analysis-api:8000", service.LastAuthority);
        Assert.Equal(Key, service.LastKey);
    }

    /// <summary>Boundary: an empty section is not a start-up error; the options are not configured and the limits are the short defaults.</summary>
    [Fact]
    public void AddMarketAnalysisService_EmptySection_RegistersUnconfiguredOptionsWithShortLimits()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMarketAnalysisService(new ConfigurationBuilder().Build());

        // Act
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<MarketAnalysisServiceOptions>();

        // Assert
        Assert.False(options.IsConfigured);
        Assert.Equal((10, 5, 3600, string.Empty), (options.TimeoutSeconds, options.PollIntervalSeconds, options.RunDeadlineSeconds, options.ApiKey));
    }

    /// <summary>Boundary: an old timeout of 90000 seconds is cut to 60, so no call of the client can stay open for long.</summary>
    [Theory]
    [InlineData("90000", 60)]
    [InlineData("60", 60)]
    [InlineData("15", 15)]
    [InlineData("0", 10)]
    [InlineData("soon", 10)]
    public void AddMarketAnalysisService_Timeout_IsNeverAbove60Seconds(string setting, int expected)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["MarketAnalysisService:BaseUrl"] = BaseUrl, ["MarketAnalysisService:TimeoutSeconds"] = setting })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMarketAnalysisService(configuration);
        using var provider = services.BuildServiceProvider();

        // Act
        var http = provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(IMarketAnalysisServiceClient));

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(expected), http.Timeout);
        Assert.Equal(expected, provider.GetRequiredService<MarketAnalysisServiceOptions>().TimeoutSeconds);
    }

    #endregion

    #region Helpers

    private static string SampleJson() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "market-analysis-result.json"));

    private static MarketAnalysisServiceOptions ApiKeyOptions() => new() { BaseUrl = BaseUrl, ApiKey = Key };

    // Replaces the run id in a path, so a test can compare the sequence of calls.
    private static string Describe(string request) =>
        request.StartsWith("POST", StringComparison.Ordinal) ? request : request.EndsWith("/result", StringComparison.Ordinal) ? "GET result" : "GET state";

    private static MarketAnalysisServiceClient CreateClient(
        FakeMarketAnalysisService service,
        MarketAnalysisServiceOptions? options = null,
        ManualTimeProvider? time = null,
        ILogger<MarketAnalysisServiceClient>? logger = null)
    {
        options ??= new MarketAnalysisServiceOptions { BaseUrl = BaseUrl };
        var http = new HttpClient(service, disposeHandler: false);
        if (options.TryGetBaseUri(out var baseUri))
        {
            http.BaseAddress = baseUri;
        }

        return new MarketAnalysisServiceClient(http, options, logger ?? NullLogger<MarketAnalysisServiceClient>.Instance, time ?? new ManualTimeProvider());
    }

    /// <summary>A log that keeps the formatted messages, to show that the API key is not in them.</summary>
    private sealed class ListLogger : ILogger<MarketAnalysisServiceClient>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception) + exception);
    }

    #endregion
}
