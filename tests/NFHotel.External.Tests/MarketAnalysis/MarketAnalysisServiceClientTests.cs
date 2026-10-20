using System.Net;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.External.Config;
using NFHotel.External.MarketAnalysis;

namespace NFHotel.External.Tests.MarketAnalysis;

/// <summary>
/// The client of the markets API (MIL-026) against a fake that follows its HTTP API: <c>POST /analyze?insights=true</c> takes a
/// JSON array of bookings and answers the result document.
/// </summary>
public sealed class MarketAnalysisServiceClientTests
{
    private static readonly MarketBookingDto Booking =
        new(7, "NF Hotel", true, 30, new DateOnly(2019, 1, 30), new DateOnly(2019, 3, 1), 1, 2, 2, 0, 0, "BB", "Portugal", "Direct", false, 0, "C", 0, "No Deposit", "Contract", 0, 1, 94.50m);

    #region Request

    /// <summary>Positive: the bookings are posted as a snake_case JSON array to the analyze path with insights switched on.</summary>
    [Fact]
    public async Task AnalyzeAsync_Bookings_PostsTheInputContractWithInsights()
    {
        // Arrange
        var service = new FakeMarketsApi();
        var client = CreateClient(service);

        // Act
        var result = await client.AnalyzeAsync([Booking], TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(("POST", "/analyze", "?insights=true", "application/json"), (service.LastMethod, service.LastPath, service.LastQuery, service.LastContentType));
        using var sent = JsonDocument.Parse(service.LastBody!);
        var row = Assert.Single(sent.RootElement.EnumerateArray());
        Assert.Equal(
            (7, "NF Hotel", true, 30, "2019-01-30", "2019-03-01", 1, 2, "Portugal", 94.5m),
            (row.GetProperty("booking_id").GetInt32(), row.GetProperty("hotel").GetString(), row.GetProperty("is_canceled").GetBoolean(), row.GetProperty("lead_time").GetInt32(), row.GetProperty("booking_date").GetString(), row.GetProperty("arrival_date").GetString(), row.GetProperty("stays_in_weekend_nights").GetInt32(), row.GetProperty("stays_in_week_nights").GetInt32(), row.GetProperty("country").GetString(), row.GetProperty("price_per_night").GetDecimal()));
        Assert.Equal("No Deposit", row.GetProperty("deposit_type").GetString());
        Assert.Equal(1, row.GetProperty("total_of_special_requests").GetInt32());
    }

    /// <summary>Boundary: a booking without market details leaves those fields out, so the markets API counts them as missing.</summary>
    [Fact]
    public async Task AnalyzeAsync_BookingWithoutDetails_LeavesTheUnknownFieldsOut()
    {
        // Arrange
        var service = new FakeMarketsApi();
        var bare = new MarketBookingDto(1, "NF Hotel", false, null, null, new DateOnly(2019, 3, 1), 0, 1, null, null, null, null, "Denmark", null, null, null, null, null, null, null, null, null, null);

        // Act
        await CreateClient(service).AnalyzeAsync([bare], TestContext.Current.CancellationToken);

        // Assert
        using var sent = JsonDocument.Parse(service.LastBody!);
        var names = sent.RootElement[0].EnumerateObject().Select(property => property.Name).Order().ToArray();
        Assert.Equal(["arrival_date", "booking_id", "country", "hotel", "is_canceled", "stays_in_week_nights", "stays_in_weekend_nights"], names);
    }

    /// <summary>Positive: no token is sent when none is set, and a bearer token when one is.</summary>
    [Fact]
    public async Task AnalyzeAsync_Token_IsSentOnlyWhenSet()
    {
        // Arrange
        var without = new FakeMarketsApi();
        var with = new FakeMarketsApi();

        // Act
        await CreateClient(without).AnalyzeAsync([Booking], TestContext.Current.CancellationToken);
        await CreateClient(with, token: "gateway-token").AnalyzeAsync([Booking], TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(without.LastAuthorization);
        Assert.Equal("Bearer gateway-token", with.LastAuthorization);
    }

    #endregion

    #region Failures

    /// <summary>Negative: each status of the markets API has its own code.</summary>
    [Theory]
    [InlineData(HttpStatusCode.UnprocessableEntity, "market_analysis_runner.input_rejected")]
    [InlineData(HttpStatusCode.Unauthorized, "market_analysis_runner.unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "market_analysis_runner.unauthorized")]
    [InlineData(HttpStatusCode.InternalServerError, "market_analysis_runner.unavailable")]
    [InlineData(HttpStatusCode.BadGateway, "market_analysis_runner.unavailable")]
    [InlineData(HttpStatusCode.UnsupportedMediaType, "market_analysis_runner.unexpected_response")]
    [InlineData(HttpStatusCode.NotFound, "market_analysis_runner.unexpected_response")]
    public async Task AnalyzeAsync_ServiceRefuses_ReturnsTheMatchingCode(HttpStatusCode status, string code)
    {
        // Arrange
        var client = CreateClient(new FakeMarketsApi { Status = status });

        // Act
        var result = await client.AnalyzeAsync([Booking], TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(code, Assert.Single(result.Errors).Code);
    }

    /// <summary>Negative: a service that cannot be reached is the unavailable code and nothing is thrown.</summary>
    [Fact]
    public async Task AnalyzeAsync_ServiceDown_ReturnsTheUnavailableCode()
    {
        // Act
        var result = await CreateClient(new FakeMarketsApi { Throw = true }).AnalyzeAsync([Booking], TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.Unavailable, Assert.Single(result.Errors).Code);
    }

    /// <summary>Negative: an answer that is not JSON, or JSON that is not a result, is the unexpected response code.</summary>
    [Theory]
    [InlineData("<html>")]
    [InlineData("[]")]
    [InlineData("{\"status\":\"completed\"}")]
    [InlineData("{\"schema_version\":\"1.1\",\"status\":\"failed\",\"error\":{\"code\":\"INPUT_ERROR\",\"message\":\"x\"}}")]
    public async Task AnalyzeAsync_AnswerIsNotAResult_ReturnsTheUnexpectedCode(string body)
    {
        // Act
        var result = await CreateClient(new FakeMarketsApi { Body = body }).AnalyzeAsync([Booking], TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.UnexpectedResponse, Assert.Single(result.Errors).Code);
    }

    /// <summary>Negative: without an address nothing is sent and the not configured code is returned.</summary>
    [Fact]
    public async Task AnalyzeAsync_NotConfigured_SendsNothing()
    {
        // Arrange
        var service = new FakeMarketsApi();
        var client = CreateClient(service, new MarketAnalysisServiceOptions());

        // Act
        var result = await client.AnalyzeAsync([Booking], TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.NotConfigured, Assert.Single(result.Errors).Code);
        Assert.Equal(0, service.Calls);
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
            () => CreateClient(new FakeMarketsApi { Throw = true, ThrowCancellation = true }).AnalyzeAsync([Booking], cancelled.Token));
    }

    #endregion

    #region Result

    /// <summary>Positive: the sample result of the contract is read into the model, the figures, the sections, the charts and the insights.</summary>
    [Fact]
    public async Task AnalyzeAsync_SampleOfTheContract_ReadsTheWholeResult()
    {
        // Arrange
        var client = CreateClient(new FakeMarketsApi { Body = SampleJson() });

        // Act
        var result = await client.AnalyzeAsync([Booking], TestContext.Current.CancellationToken);

        // Assert
        var dto = result.Value;
        Assert.Equal((1, "ollama", "phi3.5:latest"), (dto.SchemaVersion, dto.Provider, dto.Model));
        Assert.Equal(["lead_time", "holidays", "seasonality", "cancellations", "room_value", "guest_mix"], dto.Content!.Sections.Select(section => section.Id));
        Assert.Equal(["Bookings analysed", "Bookings with a price of 0", "Duplicate booking ids"], dto.Content.Figures.Select(figure => figure.Label));
        Assert.Equal((40060, 201), (dto.Content.Figures[0].Value, dto.Content.Figures[1].Value));
    }

    /// <summary>Positive: the real findings, with the group figures wrapped as <c>figure</c>, give the charts of an analysis, the overview first.</summary>
    [Fact]
    public async Task AnalyzeAsync_SampleOfTheContract_ReadsTheChartsOfTheFindings()
    {
        // Arrange
        var client = CreateClient(new FakeMarketsApi { Body = SampleJson() });

        // Act
        var sections = (await client.AnalyzeAsync([Booking], TestContext.Current.CancellationToken)).Value.Content!.Sections;

        // Assert
        var cancellations = sections.Single(section => section.Id == "cancellations");
        Assert.Contains("By deposit type", cancellations.Series.Select(series => series.Title));
        var deposit = cancellations.Series.Single(series => series.Title == "By deposit type");
        Assert.Equal("%", deposit.Unit);
        Assert.Equal(["No Deposit", "Non Refund", "Refundable"], deposit.Points.Select(point => point.Label));
        Assert.Equal(24.7, deposit.Points[0].Value);
        Assert.Equal("Overall - bands", sections.Single(section => section.Id == "lead_time").Series[0].Title);
        Assert.Empty(sections.Single(section => section.Id == "holidays").Series);
    }

    /// <summary>Positive: the AI insights are read: the executive summary and suggestions when available, the reason when not.</summary>
    [Fact]
    public async Task AnalyzeAsync_SampleOfTheContract_ReadsTheInsights()
    {
        // Arrange
        var client = CreateClient(new FakeMarketsApi { Body = SampleJson() });

        // Act
        var sections = (await client.AnalyzeAsync([Booking], TestContext.Current.CancellationToken)).Value.Content!.Sections;

        // Assert
        var cancellations = sections.Single(section => section.Id == "cancellations").Insight!;
        Assert.Equal("available", cancellations.Status);
        Assert.StartsWith("About 28% of the bookings are cancelled", cancellations.ExecutiveSummary, StringComparison.Ordinal);
        Assert.Equal([1719, 40060], cancellations.Suggestions.Select(suggestion => suggestion.SampleSize));
        Assert.Equal(("unavailable", "GUARDRAIL_REJECTED", null), (sections.Single(section => section.Id == "room_value").Insight!.Status, sections.Single(section => section.Id == "room_value").Insight!.Reason, sections.Single(section => section.Id == "room_value").Insight!.ExecutiveSummary));
    }

    /// <summary>Positive: a version 1.0 result, made without insights, is read too, with no insight in its sections.</summary>
    [Fact]
    public async Task AnalyzeAsync_Version10_ReadsWithoutInsights()
    {
        // Arrange
        var body = "{\"schema_version\":\"1.0\",\"result_id\":\"5b3f0e1a-0000-4000-8000-0000000000a1\",\"generated_at\":\"2026-10-19T10:04:00.000Z\",\"status\":\"completed\",\"analyses\":{\"guest_mix\":{\"status\":\"available\",\"findings\":{}}},\"notices\":[]}";

        // Act
        var result = await CreateClient(new FakeMarketsApi { Body = body }).AnalyzeAsync([Booking], TestContext.Current.CancellationToken);

        // Assert
        var section = Assert.Single(result.Value.Content!.Sections);
        Assert.Equal((1, null, "Guest and booking mix"), (result.Value.SchemaVersion, section.Insight, section.Title));
        Assert.Null(result.Value.Model);
    }

    #endregion

    #region Registration

    /// <summary>Positive: the registration reads the section, sets the address and the long timeout.</summary>
    [Fact]
    public async Task AddMarketAnalysisService_Configured_UsesTheAddress()
    {
        // Arrange
        var service = new FakeMarketsApi();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["MarketAnalysisService:BaseUrl"] = "http://market-analysis-api:8000" })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMarketAnalysisService(configuration);
        services.AddHttpClient<IMarketAnalysisServiceClient, MarketAnalysisServiceClient>().ConfigurePrimaryHttpMessageHandler(() => service);
        using var provider = services.BuildServiceProvider();

        // Act
        var result = await provider.GetRequiredService<IMarketAnalysisServiceClient>().AnalyzeAsync([Booking], TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(("market-analysis-api", 8000), (service.LastHost, service.LastPort));
    }

    /// <summary>Boundary: an empty section is not a start-up error; the options are not configured and the timeout is 25 minutes.</summary>
    [Fact]
    public void AddMarketAnalysisService_EmptySection_RegistersUnconfiguredOptionsWithTheLongTimeout()
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
        Assert.Equal(1500, options.TimeoutSeconds);
    }

    #endregion

    #region Helpers

    private static string SampleJson() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "market-analysis-result.json"));

    private static MarketAnalysisServiceClient CreateClient(FakeMarketsApi service, MarketAnalysisServiceOptions? options = null, string token = "")
    {
        options ??= new MarketAnalysisServiceOptions { BaseUrl = "http://market-analysis-api:8000", Token = token };
        var http = new HttpClient(service, disposeHandler: false);
        if (options.TryGetBaseUri(out var baseUri))
        {
            http.BaseAddress = baseUri;
        }

        if (!string.IsNullOrWhiteSpace(options.Token))
        {
            http.DefaultRequestHeaders.Authorization = new("Bearer", options.Token);
        }

        return new MarketAnalysisServiceClient(http, options, NullLogger<MarketAnalysisServiceClient>.Instance);
    }

    /// <summary>A fake of the markets API: records the request and answers a fixed status and body.</summary>
    private sealed class FakeMarketsApi : HttpMessageHandler
    {
        public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;

        public string? Body { get; init; }

        public bool Throw { get; init; }

        public bool ThrowCancellation { get; init; }

        public int Calls { get; private set; }

        public string LastMethod { get; private set; } = string.Empty;

        public string LastPath { get; private set; } = string.Empty;

        public string LastQuery { get; private set; } = string.Empty;

        public string LastHost { get; private set; } = string.Empty;

        public int LastPort { get; private set; }

        public string? LastAuthorization { get; private set; }

        public string? LastContentType { get; private set; }

        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastMethod = request.Method.Method;
            LastPath = request.RequestUri!.AbsolutePath;
            LastQuery = request.RequestUri.Query;
            LastHost = request.RequestUri.Host;
            LastPort = request.RequestUri.Port;
            LastAuthorization = request.Headers.Authorization?.ToString();
            LastContentType = request.Content?.Headers.ContentType?.MediaType;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            if (ThrowCancellation)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (Throw)
            {
                throw new HttpRequestException("connection refused");
            }

            if (Status != HttpStatusCode.OK)
            {
                return new HttpResponseMessage(Status);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Body ?? MinimalResult, Encoding.UTF8, "application/json"),
            };
        }

        private const string MinimalResult =
            "{\"schema_version\":\"1.1\",\"result_id\":\"5b3f0e1a-0000-4000-8000-0000000000a1\",\"generated_at\":\"2026-10-19T10:04:00.000Z\",\"status\":\"completed\",\"analyses\":{},\"notices\":[]}";
    }

    #endregion
}
