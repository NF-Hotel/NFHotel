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

    #region Adding and deleting models

    /// <summary>Positive: a pull posts the trimmed name and returns the queued download.</summary>
    [Fact]
    public async Task StartModelPullAsync_ValidName_PostsTheNameAndReturnsTheQueuedPull()
    {
        // Arrange
        var service = new FakeBenchmarkService("m1");
        var client = CreateClient(service);

        // Act
        var result = await client.StartModelPullAsync("  phi4-mini ", TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(("pull-1", "phi4-mini", ModelPullStates.Queued, (int?)null), (result.Value.PullId, result.Value.Model, result.Value.State, result.Value.Percent));
        Assert.False(result.Value.IsFinished);
        Assert.Equal("/models", service.LastPath);
        using var body = JsonDocument.Parse(service.LastBody!);
        Assert.Equal("phi4-mini", body.RootElement.GetProperty("name").GetString());
    }

    /// <summary>Negative: a name that is not an Ollama model name is refused without a call.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("../etc/passwd")]
    [InlineData("a b")]
    [InlineData("a;b")]
    public async Task StartModelPullAsync_InvalidName_ReturnsTheNameCodeWithoutACall(string name)
    {
        // Arrange
        var service = new FakeBenchmarkService();
        var client = CreateClient(service);

        // Act
        var result = await client.StartModelPullAsync(name, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.BenchmarkRunner.InvalidModelName, Assert.Single(result.Errors).Code);
        Assert.Equal(0, service.Calls);
    }

    /// <summary>Negative: an installed model and an active download are refused with their own codes.</summary>
    [Fact]
    public async Task StartModelPullAsync_InstalledOrBusy_ReturnsTheirCodes()
    {
        // Arrange
        var installed = CreateClient(new FakeBenchmarkService("phi4-mini"));
        var busy = CreateClient(new FakeBenchmarkService { PullActive = true });

        // Act
        var first = await installed.StartModelPullAsync("phi4-mini", TestContext.Current.CancellationToken);
        var second = await busy.StartModelPullAsync("qwen2.5:7b", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.BenchmarkRunner.ModelAlreadyInstalled, Assert.Single(first.Errors).Code);
        Assert.Equal(ErrorCodes.BenchmarkRunner.PullActive, Assert.Single(second.Errors).Code);
    }

    /// <summary>State: a download is read from queued through pulling to done, and the model is then listed.</summary>
    [Fact]
    public async Task GetModelPullAsync_FollowingAPull_GoesFromPullingToDoneAndTheModelIsListed()
    {
        // Arrange
        var service = new FakeBenchmarkService("m1");
        var client = CreateClient(service);
        await client.StartModelPullAsync("phi4-mini", TestContext.Current.CancellationToken);

        // Act
        var pulling = await client.GetModelPullAsync("pull-1", TestContext.Current.CancellationToken);
        var done = await client.GetModelPullAsync("pull-1", TestContext.Current.CancellationToken);
        var models = await client.ListModelsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((ModelPullStates.Pulling, 40), (pulling.Value.State, pulling.Value.Percent));
        Assert.Equal((ModelPullStates.Done, 100, true), (done.Value.State, done.Value.Percent, done.Value.IsFinished));
        Assert.Equal(["m1", "phi4-mini"], models.Value);
    }

    /// <summary>Negative: a failed download carries the reason Ollama gave.</summary>
    [Fact]
    public async Task GetModelPullAsync_FailedPull_CarriesTheReason()
    {
        // Arrange
        var client = CreateClient(new FakeBenchmarkService { FailPull = true });
        await client.StartModelPullAsync("no-such-model", TestContext.Current.CancellationToken);

        // Act
        var result = await client.GetModelPullAsync("pull-1", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ModelPullStates.Failed, result.Value.State);
        Assert.Contains("file does not exist", result.Value.Error, StringComparison.Ordinal);
        Assert.True(result.Value.IsFinished);
    }

    /// <summary>Negative: an unknown download id is the download not found code, and a blank id is refused without a call.</summary>
    [Fact]
    public async Task GetModelPullAsync_UnknownOrBlankId_ReturnsTheNotFoundCode()
    {
        // Arrange
        var service = new FakeBenchmarkService();
        var client = CreateClient(service);

        // Act
        var unknown = await client.GetModelPullAsync("nope", TestContext.Current.CancellationToken);
        var calls = service.Calls;
        var blank = await client.GetModelPullAsync(" ", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.BenchmarkRunner.PullNotFound, Assert.Single(unknown.Errors).Code);
        Assert.Equal(ErrorCodes.BenchmarkRunner.PullNotFound, Assert.Single(blank.Errors).Code);
        Assert.Equal(calls, service.Calls);
    }

    /// <summary>Positive: a delete sends DELETE with the name, a name with a tag and a path included, and the model is gone.</summary>
    [Theory]
    [InlineData("llama3.2:3b")]
    [InlineData("hf.co/user/model:Q4_K_M")]
    public async Task DeleteModelAsync_InstalledModel_DeletesItWithTheNameAsGiven(string name)
    {
        // Arrange
        var service = new FakeBenchmarkService("m1", name);
        var client = CreateClient(service);

        // Act
        var result = await client.DeleteModelAsync(name, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(["m1"], service.Installed);
    }

    /// <summary>Negative: a model that is not installed and a model that a run uses are refused with their own codes.</summary>
    [Fact]
    public async Task DeleteModelAsync_NotInstalledOrInUse_ReturnsTheirCodes()
    {
        // Arrange
        var service = new FakeBenchmarkService("used") { InUse = ["used"] };
        var client = CreateClient(service);

        // Act
        var missing = await client.DeleteModelAsync("other", TestContext.Current.CancellationToken);
        var used = await client.DeleteModelAsync("used", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.BenchmarkRunner.ModelNotFound, Assert.Single(missing.Errors).Code);
        Assert.Equal(ErrorCodes.BenchmarkRunner.ModelInUse, Assert.Single(used.Errors).Code);
        Assert.Equal(["used"], service.Installed);
    }

    /// <summary>Negative: an invalid name is refused without a call, and a 502 from the service is the model server code.</summary>
    [Fact]
    public async Task DeleteModelAsync_InvalidNameOrServerError_ReturnsTheirCodes()
    {
        // Arrange
        var service = new FakeBenchmarkService("m1");
        var client = CreateClient(service);
        var failing = CreateClient(new FakeBenchmarkService("m1") { Status = HttpStatusCode.BadGateway });

        // Act
        var invalid = await client.DeleteModelAsync("a b", TestContext.Current.CancellationToken);
        var calls = service.Calls;
        var badGateway = await failing.DeleteModelAsync("m1", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.BenchmarkRunner.InvalidModelName, Assert.Single(invalid.Errors).Code);
        Assert.Equal(0, calls);
        Assert.Equal(ErrorCodes.BenchmarkRunner.Unavailable, Assert.Single(badGateway.Errors).Code);
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

    /// <summary>Boundary: with only a token and no address set, nothing is sent; there is no built-in address.</summary>
    [Fact]
    public async Task ListModelsAsync_TokenButNoAddress_ReturnsTheNotConfiguredCodeWithoutACall()
    {
        // Arrange
        var service = new FakeBenchmarkService("m1");
        var client = CreateClient(service, new BenchmarkServiceOptions { Token = Token });

        // Act
        var result = await client.ListModelsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.BenchmarkRunner.NotConfigured, Assert.Single(result.Errors).Code);
        Assert.Equal(0, service.Calls);
        Assert.False(new BenchmarkServiceOptions { Token = Token }.IsConfigured);
    }

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
        private readonly List<string> _models;
        private int _stateReads;
        private int _pullReads;
        private string? _pulling;
        private bool _started;
        private string _startedModels = "[]";

        public FakeBenchmarkService(params string[] models) => _models = [.. models];

        public bool PullActive { get; init; }

        public bool FailPull { get; init; }

        public HashSet<string> InUse { get; init; } = [];

        public IReadOnlyList<string> Installed => _models;

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

            if (LastPath == "/models" && request.Method == HttpMethod.Post)
            {
                return StartPull();
            }

            if (LastPath.StartsWith("/models/pulls/", StringComparison.Ordinal))
            {
                return ReadPull(LastPath["/models/pulls/".Length..]);
            }

            if (LastPath.StartsWith("/models/", StringComparison.Ordinal) && request.Method == HttpMethod.Delete)
            {
                return DeleteModel(Uri.UnescapeDataString(LastPath["/models/".Length..]));
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

        private HttpResponseMessage StartPull()
        {
            using var body = JsonDocument.Parse(LastBody!);
            var name = body.RootElement.GetProperty("name").GetString()!;
            if (!ModelNames.IsValid(name))
            {
                return Reply(HttpStatusCode.BadRequest, """{"code":"invalid_model_name"}""");
            }

            if (_models.Contains(name))
            {
                return Reply(HttpStatusCode.Conflict, """{"code":"already_installed"}""");
            }

            if (PullActive)
            {
                return Reply(HttpStatusCode.Conflict, """{"code":"pull_active"}""");
            }

            _pulling = name;
            return Reply(HttpStatusCode.Accepted, Pull("queued", null, null));
        }

        private HttpResponseMessage ReadPull(string id)
        {
            if (_pulling is null || id != "pull-1")
            {
                return Reply(HttpStatusCode.NotFound, """{"code":"pull_not_found"}""");
            }

            _pullReads++;
            if (FailPull)
            {
                return Reply(HttpStatusCode.OK, Pull("failed", 40, "pull model manifest: file does not exist"));
            }

            if (_pullReads == 1)
            {
                return Reply(HttpStatusCode.OK, Pull("pulling", 40, null));
            }

            if (!_models.Contains(_pulling))
            {
                _models.Add(_pulling);
            }

            return Reply(HttpStatusCode.OK, Pull("done", 100, null));
        }

        private HttpResponseMessage DeleteModel(string name)
        {
            if (!_models.Contains(name))
            {
                return Reply(HttpStatusCode.NotFound, """{"code":"model_not_found"}""");
            }

            if (InUse.Contains(name))
            {
                return Reply(HttpStatusCode.Conflict, """{"code":"model_in_use"}""");
            }

            _models.Remove(name);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        private string Pull(string state, int? percent, string? error) =>
            JsonSerializer.Serialize(new { pull_id = "pull-1", model = _pulling, state, percent, error });

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
