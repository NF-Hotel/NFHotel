using System.Net;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.External.Benchmark;
using NFHotel.External.Config;

namespace NFHotel.External.Tests.Benchmark;

/// <summary>
/// The client of the benchmark service (MIL-020, ADR-0001 of the benchmark service) against a fake that follows the
/// contract: the token is checked, a start is validated, and a run moves from queued to running to done.
/// </summary>
public sealed class BenchmarkServiceClientTests
{
    private const string Token = "bench-token";
    private static readonly Guid RunId = Guid.Parse("5b3f0e1a-0000-4000-8000-000000000001");

    #region Models

    /// <summary>Positive: the installed models are read from GET /models with the bearer token.</summary>
    [Fact]
    public async Task ListModelsAsync_ServiceUp_ReturnsTheModelsWithTheToken()
    {
        // Arrange
        var service = new FakeBenchmarkService("llama3.2:3b", "phi3.5");
        var client = CreateClient(service);

        // Act
        var result = await client.ListModelsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(["llama3.2:3b", "phi3.5"], result.Value);
        Assert.Equal("/models", service.LastPath);
        Assert.Equal($"Bearer {Token}", service.LastAuthorization);
    }

    /// <summary>Boundary: no model installed is an empty list and not an error.</summary>
    [Fact]
    public async Task ListModelsAsync_NoModelInstalled_ReturnsAnEmptyList()
    {
        // Arrange
        var client = CreateClient(new FakeBenchmarkService());

        // Act
        var result = await client.ListModelsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    /// <summary>Negative: a wrong token is refused with the unauthorized code.</summary>
    [Fact]
    public async Task ListModelsAsync_WrongToken_ReturnsTheUnauthorizedCode()
    {
        // Arrange
        var client = CreateClient(new FakeBenchmarkService("m1"), token: "wrong");

        // Act
        var result = await client.ListModelsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.BenchmarkRunner.Unauthorized, Assert.Single(result.Errors).Code);
    }

    #endregion

    #region Starting a run

    /// <summary>Positive: a start posts the contract field names and returns the queued run.</summary>
    [Fact]
    public async Task StartRunAsync_ValidRequest_PostsTheContractBodyAndReturnsTheQueuedRun()
    {
        // Arrange
        var service = new FakeBenchmarkService("m1", "m2");
        var client = CreateClient(service);

        // Act
        var result = await client.StartRunAsync(new StartBenchmarkRunRequest(["m1", "m2"], true, true, 2), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(BenchmarkRunStates.Queued, result.Value.State);
        Assert.Equal(BenchmarkDeliveryStates.NotReady, result.Value.Delivery);
        Assert.Equal(["m1", "m2"], result.Value.Models);
        Assert.Equal(RunId, result.Value.RunId);
        using var body = JsonDocument.Parse(service.LastBody!);
        Assert.Equal(["m1", "m2"], body.RootElement.GetProperty("models").EnumerateArray().Select(item => item.GetString()));
        Assert.True(body.RootElement.GetProperty("hotel_analysis").GetBoolean());
        Assert.True(body.RootElement.GetProperty("deterministic_tasks").GetBoolean());
        Assert.Equal(2, body.RootElement.GetProperty("runs").GetInt32());
    }

    /// <summary>Negative: a model that is not installed is refused with its own code.</summary>
    [Fact]
    public async Task StartRunAsync_ModelNotInstalled_ReturnsTheModelCode()
    {
        // Arrange
        var client = CreateClient(new FakeBenchmarkService("m1"));

        // Act
        var result = await client.StartRunAsync(new StartBenchmarkRunRequest(["other"]), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.BenchmarkRunner.ModelNotInstalled, Assert.Single(result.Errors).Code);
    }

    /// <summary>Negative: a request out of range is refused with the invalid request code.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task StartRunAsync_RunsOutOfRange_ReturnsTheInvalidRequestCode(int runs)
    {
        // Arrange
        var client = CreateClient(new FakeBenchmarkService("m1"));

        // Act
        var result = await client.StartRunAsync(new StartBenchmarkRunRequest(["m1"], Runs: runs), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.BenchmarkRunner.InvalidRequest, Assert.Single(result.Errors).Code);
    }

    /// <summary>Negative: a full queue is answered 429 and mapped to the queue full code.</summary>
    [Fact]
    public async Task StartRunAsync_QueueFull_ReturnsTheQueueFullCode()
    {
        // Arrange
        var service = new FakeBenchmarkService("m1") { QueueFull = true };
        var client = CreateClient(service);

        // Act
        var result = await client.StartRunAsync(new StartBenchmarkRunRequest(["m1"]), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.BenchmarkRunner.QueueFull, Assert.Single(result.Errors).Code);
    }

    /// <summary>Negative: a start with a wrong token is refused with the unauthorized code.</summary>
    [Fact]
    public async Task StartRunAsync_WrongToken_ReturnsTheUnauthorizedCode()
    {
        // Arrange
        var client = CreateClient(new FakeBenchmarkService("m1"), token: "wrong");

        // Act
        var result = await client.StartRunAsync(new StartBenchmarkRunRequest(["m1"]), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.BenchmarkRunner.Unauthorized, Assert.Single(result.Errors).Code);
    }

    /// <summary>Negative: a request without models is not sent at all.</summary>
    [Fact]
    public async Task StartRunAsync_NoModels_ReturnsTheInvalidRequestCodeWithoutACall()
    {
        // Arrange
        var service = new FakeBenchmarkService("m1");
        var client = CreateClient(service);

        // Act
        var result = await client.StartRunAsync(new StartBenchmarkRunRequest([]), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.BenchmarkRunner.InvalidRequest, Assert.Single(result.Errors).Code);
        Assert.Equal(0, service.Calls);
    }

    #endregion

    #region Following a run

    /// <summary>State transition: a started run is read as queued, then running, then done with its delivery.</summary>
    [Fact]
    public async Task GetRunAsync_StartedRun_MovesFromQueuedToRunningToDone()
    {
        // Arrange
        var service = new FakeBenchmarkService("m1");
        var client = CreateClient(service);
        await client.StartRunAsync(new StartBenchmarkRunRequest(["m1"]), TestContext.Current.CancellationToken);

        // Act
        var first = await client.GetRunAsync(RunId, TestContext.Current.CancellationToken);
        var second = await client.GetRunAsync(RunId, TestContext.Current.CancellationToken);
        var third = await client.GetRunAsync(RunId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(BenchmarkRunStates.Running, first.Value.State);
        Assert.False(first.Value.IsFinished);
        Assert.Equal(BenchmarkRunStates.Done, second.Value.State);
        Assert.True(second.Value.IsFinished);
        Assert.Equal(BenchmarkDeliveryStates.Pending, second.Value.Delivery);
        Assert.Equal(BenchmarkDeliveryStates.Delivered, third.Value.Delivery);
        Assert.NotNull(third.Value.FinishedAt);
    }

    /// <summary>Negative: a run the service does not know is the run not found code.</summary>
    [Fact]
    public async Task GetRunAsync_UnknownRun_ReturnsTheRunNotFoundCode()
    {
        // Arrange
        var client = CreateClient(new FakeBenchmarkService("m1"));

        // Act
        var result = await client.GetRunAsync(RunId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.BenchmarkRunner.RunNotFound, Assert.Single(result.Errors).Code);
    }

    /// <summary>Negative: the empty run id is not sent.</summary>
    [Fact]
    public async Task GetRunAsync_EmptyRunId_ReturnsTheRunNotFoundCodeWithoutACall()
    {
        // Arrange
        var service = new FakeBenchmarkService("m1");
        var client = CreateClient(service);

        // Act
        var result = await client.GetRunAsync(Guid.Empty, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.BenchmarkRunner.RunNotFound, Assert.Single(result.Errors).Code);
        Assert.Equal(0, service.Calls);
    }

    #endregion

    #region When the service cannot be used

    /// <summary>Negative: a service that cannot be reached is the unavailable code, never an exception.</summary>
    [Fact]
    public async Task ListModelsAsync_ServiceDown_ReturnsTheUnavailableCode()
    {
        // Arrange
        var client = CreateClient(new FakeBenchmarkService("m1") { Throw = new HttpRequestException("refused") });

        // Act
        var result = await client.ListModelsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.BenchmarkRunner.Unavailable, Assert.Single(result.Errors).Code);
    }

    /// <summary>Negative: a timeout that the caller did not ask for is the unavailable code.</summary>
    [Fact]
    public async Task ListModelsAsync_Timeout_ReturnsTheUnavailableCode()
    {
        // Arrange
        var client = CreateClient(new FakeBenchmarkService("m1") { Throw = new TaskCanceledException("timeout") });

        // Act
        var result = await client.ListModelsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.BenchmarkRunner.Unavailable, Assert.Single(result.Errors).Code);
    }

    /// <summary>Negative: a server error is the unavailable code and an unreadable body is the unexpected response code.</summary>
    [Fact]
    public async Task ListModelsAsync_ServerErrorAndGarbage_MapToUnavailableAndUnexpected()
    {
        // Arrange
        var broken = CreateClient(new FakeBenchmarkService("m1") { Status = HttpStatusCode.InternalServerError });
        var garbage = CreateClient(new FakeBenchmarkService("m1") { RawBody = "not json" });

        // Act
        var serverError = await broken.ListModelsAsync(TestContext.Current.CancellationToken);
        var unreadable = await garbage.ListModelsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.BenchmarkRunner.Unavailable, Assert.Single(serverError.Errors).Code);
        Assert.Equal(ErrorCodes.BenchmarkRunner.UnexpectedResponse, Assert.Single(unreadable.Errors).Code);
    }

    /// <summary>Negative: without an address or a token nothing is sent and the not configured code is returned.</summary>
    [Theory]
    [InlineData("", "http://benchmark:8000")]
    [InlineData("bench-token", "not a url")]
    public async Task ListModelsAsync_NotConfigured_ReturnsTheNotConfiguredCodeWithoutACall(string token, string baseUrl)
    {
        // Arrange
        var service = new FakeBenchmarkService("m1");
        var client = CreateClient(service, new BenchmarkServiceOptions { Token = token, BaseUrl = baseUrl });

        // Act
        var result = await client.ListModelsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.BenchmarkRunner.NotConfigured, Assert.Single(result.Errors).Code);
        Assert.Equal(0, service.Calls);
    }

    #endregion

    #region Registration

    /// <summary>Positive: the registration reads the section, sets the address and sends the token as a bearer token.</summary>
    [Fact]
    public async Task AddBenchmarkService_Configured_SendsTheBearerTokenToTheConfiguredAddress()
    {
        // Arrange
        var service = new FakeBenchmarkService("m1");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BenchmarkService:BaseUrl"] = "http://benchmark.example:9000/",
                ["BenchmarkService:Token"] = Token,
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBenchmarkService(configuration);
        services.AddHttpClient<IBenchmarkServiceClient, BenchmarkServiceClient>().ConfigurePrimaryHttpMessageHandler(() => service);
        using var provider = services.BuildServiceProvider();

        // Act
        var result = await provider.GetRequiredService<IBenchmarkServiceClient>().ListModelsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("http://benchmark.example:9000/models", service.LastUri);
        Assert.Equal($"Bearer {Token}", service.LastAuthorization);
    }

    #endregion

    #region Helpers

    private static BenchmarkServiceClient CreateClient(FakeBenchmarkService service, BenchmarkServiceOptions? options = null, string token = Token)
    {
        options ??= new BenchmarkServiceOptions { BaseUrl = "http://benchmark:8000", Token = token };
        var http = new HttpClient(service, disposeHandler: false);
        if (options.TryGetBaseUri(out var baseUri))
        {
            http.BaseAddress = baseUri;
        }

        if (!string.IsNullOrWhiteSpace(options.Token))
        {
            http.DefaultRequestHeaders.Authorization = new("Bearer", options.Token);
        }

        return new BenchmarkServiceClient(http, options, NullLogger<BenchmarkServiceClient>.Instance);
    }

    /// <summary>A fake of the benchmark service that follows <c>openapi/benchmark-contract.yaml</c>.</summary>
    private sealed class FakeBenchmarkService : HttpMessageHandler
    {
        private readonly string[] _models;
        private int _stateReads;
        private bool _started;
        private string _startedModels = "[]";

        public FakeBenchmarkService(params string[] models) => _models = models;

        public int Calls { get; private set; }

        public string? LastPath { get; private set; }

        public string? LastUri { get; private set; }

        public string? LastAuthorization { get; private set; }

        public string? LastBody { get; private set; }

        public bool QueueFull { get; init; }

        public Exception? Throw { get; init; }

        public HttpStatusCode? Status { get; init; }

        public string? RawBody { get; init; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastPath = request.RequestUri!.AbsolutePath;
            LastUri = request.RequestUri.ToString();
            LastAuthorization = request.Headers.Authorization?.ToString();
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            if (Throw is not null)
            {
                throw Throw;
            }

            if (Status is { } status)
            {
                return Reply(status, """{"code":"boom"}""");
            }

            if (RawBody is not null)
            {
                return Reply(HttpStatusCode.OK, RawBody);
            }

            if (LastAuthorization != $"Bearer {Token}")
            {
                return Reply(HttpStatusCode.Unauthorized, """{"code":"unauthorized"}""");
            }

            return LastPath switch
            {
                "/models" => Reply(HttpStatusCode.OK, JsonSerializer.Serialize(new { models = _models })),
                "/runs" when request.Method == HttpMethod.Post => Start(),
                _ when LastPath.StartsWith("/runs/", StringComparison.Ordinal) => Read(LastPath["/runs/".Length..]),
                _ => Reply(HttpStatusCode.NotFound, """{"code":"not_found"}"""),
            };
        }

        private static HttpResponseMessage Reply(HttpStatusCode status, string json) =>
            new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

        private HttpResponseMessage Start()
        {
            if (QueueFull)
            {
                var full = Reply((HttpStatusCode)429, """{"code":"queue_full"}""");
                full.Headers.Add("Retry-After", "30");
                return full;
            }

            using var body = JsonDocument.Parse(LastBody!);
            var asked = body.RootElement.GetProperty("models").EnumerateArray().Select(item => item.GetString()!).ToArray();
            var runs = body.RootElement.GetProperty("runs").GetInt32();
            if (asked.Length == 0 || runs is < 1 or > 5)
            {
                return Reply(HttpStatusCode.BadRequest, """{"code":"invalid_request"}""");
            }

            if (asked.Any(model => !_models.Contains(model)))
            {
                return Reply(HttpStatusCode.BadRequest, """{"code":"model_not_installed"}""");
            }

            _started = true;
            _startedModels = JsonSerializer.Serialize(asked);
            return Reply(HttpStatusCode.Accepted, State("queued", "not_ready", withTimes: false));
        }

        private HttpResponseMessage Read(string id)
        {
            if (!_started || id != "5b3f0e1a-0000-4000-8000-000000000001")
            {
                return Reply(HttpStatusCode.NotFound, """{"code":"run_not_found"}""");
            }

            _stateReads++;
            return _stateReads switch
            {
                1 => Reply(HttpStatusCode.OK, State("running", "not_ready", withTimes: false)),
                2 => Reply(HttpStatusCode.OK, State("done", "pending", withTimes: true)),
                _ => Reply(HttpStatusCode.OK, State("done", "delivered", withTimes: true)),
            };
        }

        private string State(string state, string delivery, bool withTimes) =>
            "{\"run_id\":\"5b3f0e1a-0000-4000-8000-000000000001\",\"state\":\"" + state + "\",\"delivery\":\"" + delivery
            + "\",\"models\":" + _startedModels + ",\"queued_at\":\"2026-10-19T10:00:00+00:00\","
            + (withTimes
                ? "\"started_at\":\"2026-10-19T10:00:05+00:00\",\"finished_at\":\"2026-10-19T10:05:00+00:00\""
                : "\"started_at\":null,\"finished_at\":null")
            + ",\"error\":null}";
    }

    #endregion
}