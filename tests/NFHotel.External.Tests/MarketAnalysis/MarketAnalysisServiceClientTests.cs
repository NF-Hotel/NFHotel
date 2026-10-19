using System.Net;
using System.Text;

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
/// The client of the data analysis service (MIL-026) against a fake that follows its API: the token is checked, a start
/// answers 202 with a job id, and a run moves from queued to completed.
/// </summary>
public sealed class MarketAnalysisServiceClientTests
{
    private const string Token = "analysis-token";
    private static readonly Guid JobId = Guid.Parse("5b3f0e1a-0000-4000-8000-0000000000a1");

    #region Start

    /// <summary>Positive: a start posts to the market analysis notebook with the bearer token and returns the queued run.</summary>
    [Fact]
    public async Task StartRunAsync_ServiceUp_PostsToTheNotebookWithTheToken()
    {
        // Arrange
        var service = new FakeAnalysisService();
        var client = CreateClient(service);

        // Act
        var result = await client.StartRunAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal((JobId, MarketAnalysisRunStates.Queued), (result.Value.RunId, result.Value.State));
        Assert.Equal(("POST", "/api/v1/notebooks/market_analysis/runs", $"Bearer {Token}"), (service.LastMethod, service.LastPath, service.LastAuthorization));
    }

    /// <summary>Negative: each status of the service has its own code.</summary>
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "market_analysis_runner.unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "market_analysis_runner.unauthorized")]
    [InlineData(HttpStatusCode.Conflict, "market_analysis_runner.already_running")]
    [InlineData(HttpStatusCode.TooManyRequests, "market_analysis_runner.rate_limited")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "market_analysis_runner.unavailable")]
    [InlineData(HttpStatusCode.BadRequest, "market_analysis_runner.unexpected_response")]
    public async Task StartRunAsync_ServiceRefuses_ReturnsTheMatchingCode(HttpStatusCode status, string code)
    {
        // Arrange
        var client = CreateClient(new FakeAnalysisService { Status = status });

        // Act
        var result = await client.StartRunAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(code, Assert.Single(result.Errors).Code);
    }

    /// <summary>Negative: a wrong token is refused by the fake with the unauthorized code.</summary>
    [Fact]
    public async Task StartRunAsync_WrongToken_ReturnsTheUnauthorizedCode()
    {
        // Arrange
        var client = CreateClient(new FakeAnalysisService(), token: "wrong");

        // Act
        var result = await client.StartRunAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.Unauthorized, Assert.Single(result.Errors).Code);
    }

    /// <summary>Negative: a service that cannot be reached is the unavailable code and nothing is thrown.</summary>
    [Fact]
    public async Task StartRunAsync_ServiceDown_ReturnsTheUnavailableCode()
    {
        // Arrange
        var client = CreateClient(new FakeAnalysisService { Throw = true });

        // Act
        var result = await client.StartRunAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.Unavailable, Assert.Single(result.Errors).Code);
    }

    /// <summary>Negative: an answer that is not JSON is the unexpected response code.</summary>
    [Fact]
    public async Task StartRunAsync_AnswerIsNotJson_ReturnsTheUnexpectedCode()
    {
        // Arrange
        var client = CreateClient(new FakeAnalysisService { Body = "<html>" });

        // Act
        var result = await client.StartRunAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.UnexpectedResponse, Assert.Single(result.Errors).Code);
    }

    /// <summary>Negative: without a token or an address nothing is sent and the not configured code is returned.</summary>
    [Fact]
    public async Task StartRunAsync_NotConfigured_SendsNothing()
    {
        // Arrange
        var service = new FakeAnalysisService();
        var client = CreateClient(service, new MarketAnalysisServiceOptions { BaseUrl = "http://analysis:8000" });

        // Act
        var result = await client.StartRunAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.NotConfigured, Assert.Single(result.Errors).Code);
        Assert.Equal(0, service.Calls);
    }

    #endregion

    #region State

    /// <summary>Positive: the state of a run is read with its times and its sanitised error.</summary>
    [Fact]
    public async Task GetRunAsync_FailedRun_ReturnsTheStateWithTheError()
    {
        // Arrange
        var service = new FakeAnalysisService
        {
            Body = $$$"""{"job_id":"{{{JobId}}}","notebook":"market_analysis","status":"failed","started_at":"2026-10-19T10:00:00Z","finished_at":"2026-10-19T10:01:00Z","error":{"code":"notebook_execution_failed","message":"The notebook failed."}}""",
        };
        var client = CreateClient(service);

        // Act
        var result = await client.GetRunAsync(JobId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(($"/api/v1/runs/{JobId}", MarketAnalysisRunStates.Failed, "The notebook failed."), (service.LastPath, result.Value.State, result.Value.Error));
        Assert.True(result.Value.IsFinished);
        Assert.Equal(new DateTimeOffset(2026, 10, 19, 10, 0, 0, TimeSpan.Zero), result.Value.StartedAt);
    }

    /// <summary>Positive: an error that is a plain string is read as it is.</summary>
    [Fact]
    public async Task GetRunAsync_ErrorAsString_ReturnsTheString()
    {
        // Arrange
        var client = CreateClient(new FakeAnalysisService { Body = $$"""{"job_id":"{{JobId}}","status":"failed","error":"interrupted"}""" });

        // Act
        var result = await client.GetRunAsync(JobId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("interrupted", result.Value.Error);
    }

    /// <summary>Negative: a run the service has deleted is the run not found code, and the empty id is not sent.</summary>
    [Fact]
    public async Task GetRunAsync_UnknownRunAndEmptyId_AreNotFound()
    {
        // Arrange
        var service = new FakeAnalysisService { Status = HttpStatusCode.NotFound };
        var client = CreateClient(service);

        // Act
        var unknown = await client.GetRunAsync(JobId, TestContext.Current.CancellationToken);
        var empty = await client.GetRunAsync(Guid.Empty, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.RunNotFound, Assert.Single(unknown.Errors).Code);
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.RunNotFound, Assert.Single(empty.Errors).Code);
        Assert.Equal(1, service.Calls);
    }

    /// <summary>Negative: a state without a job id is an unexpected response.</summary>
    [Fact]
    public async Task GetRunAsync_NoJobId_ReturnsTheUnexpectedCode()
    {
        // Arrange
        var client = CreateClient(new FakeAnalysisService { Body = """{"status":"queued"}""" });

        // Act
        var result = await client.GetRunAsync(JobId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.UnexpectedResponse, Assert.Single(result.Errors).Code);
    }

    #endregion

    #region Result

    /// <summary>Positive: the sample result of the contract is read into the header, the summary, the figures and the series.</summary>
    [Fact]
    public async Task GetResultAsync_SampleOfTheContract_ReadsTheWholeResult()
    {
        // Arrange
        var service = new FakeAnalysisService { Body = SampleJson() };
        var client = CreateClient(service);

        // Act
        var result = await client.GetResultAsync(JobId, TestContext.Current.CancellationToken);

        // Assert
        var dto = result.Value;
        Assert.Equal($"/api/v1/runs/{JobId}/result", service.LastPath);
        Assert.Equal((JobId, 1, "ollama", "phi3.5:latest"), (dto.RunId, dto.SchemaVersion, dto.Provider, dto.Model));
        Assert.Equal(new DateTimeOffset(2026, 10, 19, 10, 4, 0, TimeSpan.Zero), dto.FinishedAt);
        Assert.StartsWith("Portugal", dto.Content!.Summary, StringComparison.Ordinal);
        Assert.Equal(("Cancellation rate", 37.1, "%"), (dto.Content.Figures[0].Label, dto.Content.Figures[0].Value, dto.Content.Figures[0].Unit));
        Assert.Equal(["Portugal", "United Kingdom"], dto.Content.Series[0].Points.Select(point => point.Label));
        Assert.Equal(48590, dto.Content.Series[0].Points[0].Value);
    }

    /// <summary>Boundary: a result without an AI summary or a model is read, with an empty summary and no model.</summary>
    [Fact]
    public async Task GetResultAsync_NoAiAnalysis_ReadsAnEmptySummary()
    {
        // Arrange
        var client = CreateClient(new FakeAnalysisService
        {
            Body = """{"metadata":{"started_at":"2026-10-19T10:00:00Z","finished_at":"2026-10-19T10:01:00Z"},"schema_version":1,"result":{"figures":[],"series":[]}}""",
        });

        // Act
        var result = await client.GetResultAsync(JobId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result.Value.Model);
        Assert.Equal(string.Empty, result.Value.Content!.Summary);
    }

    /// <summary>Negative: a result without a schema version or a result part is an unexpected response.</summary>
    [Theory]
    [InlineData("""{"metadata":{},"result":{}}""")]
    [InlineData("""{"metadata":{},"schema_version":1}""")]
    [InlineData("""{"schema_version":1,"result":{}}""")]
    public async Task GetResultAsync_MissingPart_ReturnsTheUnexpectedCode(string body)
    {
        // Arrange
        var client = CreateClient(new FakeAnalysisService { Body = body });

        // Act
        var result = await client.GetResultAsync(JobId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.UnexpectedResponse, Assert.Single(result.Errors).Code);
    }

    #endregion

    #region Registration

    /// <summary>Positive: the registration reads the section, sets the address and sends the token as a bearer token.</summary>
    [Fact]
    public async Task AddMarketAnalysisService_Configured_SendsTheBearerTokenToTheConfiguredAddress()
    {
        // Arrange
        var service = new FakeAnalysisService();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MarketAnalysisService:BaseUrl"] = "http://analysis.example:9000",
                ["MarketAnalysisService:Token"] = Token,
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMarketAnalysisService(configuration);
        services.AddHttpClient<IMarketAnalysisServiceClient, MarketAnalysisServiceClient>().ConfigurePrimaryHttpMessageHandler(() => service);
        using var provider = services.BuildServiceProvider();

        // Act
        var result = await provider.GetRequiredService<IMarketAnalysisServiceClient>().StartRunAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(("analysis.example", 9000, $"Bearer {Token}"), (service.LastHost, service.LastPort, service.LastAuthorization));
    }

    /// <summary>Boundary: an empty section is not a start-up error, and the options say they are not configured.</summary>
    [Fact]
    public void AddMarketAnalysisService_EmptySection_RegistersUnconfiguredOptions()
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
        Assert.Equal(MarketAnalysisServiceOptions.DefaultTimeoutSeconds, options.TimeoutSeconds);
    }

    #endregion

    #region Helpers

    private static string SampleJson() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData", "market-analysis-result.json"));

    private static MarketAnalysisServiceClient CreateClient(FakeAnalysisService service, MarketAnalysisServiceOptions? options = null, string token = Token)
    {
        options ??= new MarketAnalysisServiceOptions { BaseUrl = "http://analysis:8000", Token = token };
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

    /// <summary>A fake of the data analysis service: checks the token, then answers a fixed status and body.</summary>
    private sealed class FakeAnalysisService : HttpMessageHandler
    {
        public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;

        public string? Body { get; init; }

        public bool Throw { get; init; }

        public int Calls { get; private set; }

        public string LastMethod { get; private set; } = string.Empty;

        public string LastPath { get; private set; } = string.Empty;

        public string LastHost { get; private set; } = string.Empty;

        public int LastPort { get; private set; }

        public string? LastAuthorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastMethod = request.Method.Method;
            LastPath = request.RequestUri!.AbsolutePath;
            LastHost = request.RequestUri.Host;
            LastPort = request.RequestUri.Port;
            LastAuthorization = request.Headers.Authorization?.ToString();

            if (Throw)
            {
                throw new HttpRequestException("connection refused");
            }

            if (LastAuthorization != $"Bearer {Token}")
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            }

            if (Status != HttpStatusCode.OK)
            {
                return Task.FromResult(new HttpResponseMessage(Status));
            }

            var body = Body ?? $$"""{"job_id":"{{JobId}}","notebook":"market_analysis","status":"queued"}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }

    #endregion
}
