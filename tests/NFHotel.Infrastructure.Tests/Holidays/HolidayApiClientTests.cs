using System.Net;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NFHotel.Application.Dto.Holidays;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Holidays;

namespace NFHotel.Infrastructure.Tests.Holidays;

/// <summary>
/// Tests <see cref="HolidayApiClient"/> against a fake HTTP handler — no network, no running service.
/// </summary>
public sealed class HolidayApiClientTests
{
    private const string TwoHolidays = """
        {
          "country": "KH",
          "source": "python-holidays 0.104",
          "first_year": 2022,
          "last_year": 2027,
          "holidays": [
            { "name": "International New Year Day", "start_date": "2024-01-01", "end_date": "2024-01-01" },
            { "name": "Khmer New Year's Day", "start_date": "2024-04-13", "end_date": "2024-04-16" }
          ]
        }
        """;

    [Fact]
    public async Task GetAsync_WithoutYear_CallsHolidaysAndMapsTheBody()
    {
        var handler = StubHandler.Json(TwoHolidays);
        var client = CreateClient(handler);

        var result = await client.GetAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Get, handler.Request!.Method);
        Assert.Equal("http://127.0.0.1:8000/api/v1/holidays", handler.Request.RequestUri!.ToString());

        var calendar = result.Value;
        Assert.Equal("KH", calendar.Country);
        Assert.Equal("python-holidays 0.104", calendar.Source);
        Assert.Equal(2022, calendar.FirstYear);
        Assert.Equal(2027, calendar.LastYear);
        Assert.Equal(2, calendar.Holidays.Count);
        Assert.Equal(
            new HolidayPeriodDto("Khmer New Year's Day", new DateOnly(2024, 4, 13), new DateOnly(2024, 4, 16)),
            calendar.Holidays[1]);
    }

    [Fact]
    public async Task GetAsync_WithYear_AddsYearToTheQuery()
    {
        var handler = StubHandler.Json(TwoHolidays);

        await CreateClient(handler).GetAsync(2024);

        Assert.Equal("?year=2024", handler.Request!.RequestUri!.Query);
    }

    [Fact]
    public async Task RefreshAsync_WithoutYears_PostsToUpdateWithoutQuery()
    {
        var handler = StubHandler.Json(TwoHolidays);

        var result = await CreateClient(handler).RefreshAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("/api/v1/holidays/update", handler.Request.RequestUri!.AbsolutePath);
        Assert.Equal(string.Empty, handler.Request.RequestUri.Query);
    }

    [Fact]
    public async Task RefreshAsync_WithYears_PostsFirstAndLastYear()
    {
        var handler = StubHandler.Json(TwoHolidays);

        await CreateClient(handler).RefreshAsync(2022, 2027);

        Assert.Equal("?first_year=2022&last_year=2027", handler.Request!.RequestUri!.Query);
    }

    [Fact]
    public async Task RefreshAsync_OnlyLastYear_SendsOnlyLastYear()
    {
        var handler = StubHandler.Json(TwoHolidays);

        await CreateClient(handler).RefreshAsync(lastYear: 2030);

        Assert.Equal("?last_year=2030", handler.Request!.RequestUri!.Query);
    }

    [Fact]
    public async Task RefreshAsync_FirstYearAfterLastYear_FailsWithoutCallingTheService()
    {
        var handler = StubHandler.Json(TwoHolidays);

        var result = await CreateClient(handler).RefreshAsync(2027, 2022);

        Assert.Equal(ErrorCodes.Holidays.InvalidRequest, result.FirstError!.Code);
        Assert.Null(handler.Request);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Calls_WithoutApiKey_FailAsNotConfiguredWithoutCallingTheService(string apiKey)
    {
        var handler = StubHandler.Json(TwoHolidays);
        var client = CreateClient(handler, new HolidayApiOptions { ApiKey = apiKey });

        var result = await client.GetAsync();

        Assert.Equal(ErrorCodes.Holidays.NotConfigured, result.FirstError!.Code);
        Assert.Null(handler.Request);
    }

    [Fact]
    public async Task Calls_WithInvalidBaseUrl_FailAsNotConfigured()
    {
        var handler = StubHandler.Json(TwoHolidays);
        var client = CreateClient(handler, new HolidayApiOptions { ApiKey = "k", BaseUrl = "not a url" });

        var result = await client.GetAsync();

        Assert.Equal(ErrorCodes.Holidays.NotConfigured, result.FirstError!.Code);
        Assert.Null(handler.Request);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, ErrorCodes.Holidays.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, ErrorCodes.Holidays.Unauthorized)]
    [InlineData(HttpStatusCode.BadRequest, ErrorCodes.Holidays.InvalidRequest)]
    [InlineData(HttpStatusCode.UnprocessableEntity, ErrorCodes.Holidays.InvalidRequest)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorCodes.Holidays.Unavailable)]
    [InlineData(HttpStatusCode.ServiceUnavailable, ErrorCodes.Holidays.Unavailable)]
    [InlineData(HttpStatusCode.NotFound, ErrorCodes.Holidays.UnexpectedResponse)]
    public async Task ErrorStatuses_AreMappedToErrorCodes(HttpStatusCode status, string expectedCode)
    {
        var handler = StubHandler.Status(status);

        var get = await CreateClient(handler).GetAsync();
        var refresh = await CreateClient(handler).RefreshAsync();

        Assert.Equal(expectedCode, get.FirstError!.Code);
        Assert.Equal(expectedCode, refresh.FirstError!.Code);
    }

    [Fact]
    public async Task NetworkFailure_IsUnavailable()
    {
        var handler = StubHandler.Throwing(new HttpRequestException("connection refused"));

        var result = await CreateClient(handler).GetAsync();

        Assert.Equal(ErrorCodes.Holidays.Unavailable, result.FirstError!.Code);
    }

    [Fact]
    public async Task Timeout_IsUnavailable()
    {
        // HttpClient signals its own timeout as TaskCanceledException without the caller's token being cancelled.
        var handler = StubHandler.Throwing(new TaskCanceledException("timeout"));

        var result = await CreateClient(handler).GetAsync();

        Assert.Equal(ErrorCodes.Holidays.Unavailable, result.FirstError!.Code);
    }

    [Fact]
    public async Task CallerCancellation_IsNotSwallowed()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var handler = StubHandler.Throwing(new TaskCanceledException("cancelled"));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateClient(handler).GetAsync(cancellationToken: cts.Token));
    }

    [Theory]
    [InlineData("this is not json")]
    [InlineData("null")]
    [InlineData("""{ "country": "KH", "source": "x", "first_year": 2022, "last_year": 2027 }""")]
    [InlineData("""{ "country": "KH", "first_year": 2022, "last_year": 2027, "holidays": [] }""")]
    public async Task UnreadableOrIncompleteBody_IsUnexpectedResponse(string body)
    {
        var handler = StubHandler.Json(body);

        var result = await CreateClient(handler).GetAsync();

        Assert.Equal(ErrorCodes.Holidays.UnexpectedResponse, result.FirstError!.Code);
    }

    [Fact]
    public async Task EmptyCalendar_IsSuccess()
    {
        var handler = StubHandler.Json(
            """{ "country": "KH", "source": "x", "first_year": 2022, "last_year": 2027, "holidays": [] }""");

        var result = await CreateClient(handler).GetAsync(1999);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Holidays);
    }

    [Fact]
    public async Task Registration_SendsApiKeyHeaderToTheConfiguredBaseUrl()
    {
        var handler = StubHandler.Json(TwoHolidays);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:HotelDatabase"] = "Host=localhost;Database=unused",
                ["Encryption:PassportKey"] = Convert.ToBase64String(new byte[32]),
                ["HolidayApi:BaseUrl"] = "http://holidays.example:9000/",
                ["HolidayApi:ApiKey"] = "secret-key",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        services.AddHttpClient<IHolidayCalendarClient, HolidayApiClient>()
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHolidayCalendarClient>();

        var result = await client.GetAsync(2024);

        Assert.True(result.IsSuccess);
        Assert.Equal("http://holidays.example:9000/api/v1/holidays?year=2024", handler.Request!.RequestUri!.ToString());
        Assert.Equal("secret-key", Assert.Single(handler.Request.Headers.GetValues("X-API-Key")));
    }

    private static HolidayApiClient CreateClient(StubHandler handler, HolidayApiOptions? options = null)
    {
        options ??= new HolidayApiOptions { ApiKey = "test-key" };
        var http = new HttpClient(handler, disposeHandler: false);

        if (options.TryGetBaseUri(out var baseUri))
        {
            http.BaseAddress = baseUri;
        }

        return new HolidayApiClient(http, options, NullLogger<HolidayApiClient>.Instance);
    }

    /// <summary>Fake handler: remembers the last call and answers with what it has been asked to.</summary>
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage> _respond;

        private StubHandler(Func<HttpResponseMessage> respond) => _respond = respond;

        public HttpRequestMessage? Request { get; private set; }

        public static StubHandler Json(string body) => new(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });

        public static StubHandler Status(HttpStatusCode status) => new(() => new HttpResponseMessage(status)
        {
            Content = new StringContent("""{"detail":"nope"}""", Encoding.UTF8, "application/json"),
        });

        public static StubHandler Throwing(Exception exception) => new(() => throw exception);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;

            return Task.FromResult(_respond());
        }
    }
}