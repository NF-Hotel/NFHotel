using System.Net;
using System.Net.Http.Json;

using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Errors;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Api.Tests;

/// <summary>
/// The benchmark endpoints (ADR-0013): the benchmark service posts a run with the service token only, and a Manager
/// reads the stored runs. Every refusal happens before the use case is called.
/// </summary>
public sealed class BenchmarkEndpointTests
{
    private const string Results = "/api/benchmark/results";
    private const string Runs = "/api/benchmark/runs";
    private static readonly Guid RunId = Guid.Parse("5b3f0e1a-0000-4000-8000-000000000001");
    private static readonly DateTimeOffset Start = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);

    #region Receiving a run

    /// <summary>Positive: a new run is answered 201 and stored once.</summary>
    [Fact]
    public async Task PostResult_NewRun_Returns201AndStoresIt()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateServiceClient();

        // Act
        using var response = await client.PostAsJsonAsync(Results, Body(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Single(factory.World.Benchmarks.Items);
    }

    /// <summary>Positive: the same run posted again is answered 200 and nothing new is stored.</summary>
    [Fact]
    public async Task PostResult_SameRunTwice_Returns201ThenAn200WithoutStoringAgain()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateServiceClient();
        using var first = await client.PostAsJsonAsync(Results, Body(), TestContext.Current.CancellationToken);

        // Act
        using var second = await client.PostAsJsonAsync(Results, Body(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Single(factory.World.Benchmarks.Items);
    }

    /// <summary>Negative: a post without the service token is 401 and stores nothing.</summary>
    [Fact]
    public async Task PostResult_WithoutTheServiceToken_Returns401AndStoresNothing()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(Results, Body(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(factory.World.Benchmarks.Items);
    }

    /// <summary>Negative: a post with a wrong token is 401.</summary>
    [Fact]
    public async Task PostResult_WithAWrongServiceToken_Returns401()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", "not-the-token");

        // Act
        using var response = await client.PostAsJsonAsync(Results, Body(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Negative: an unknown schema version is 400 with its code and stores nothing.</summary>
    [Fact]
    public async Task PostResult_UnknownSchemaVersion_Returns400WithTheCode()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateServiceClient();

        // Act
        using var response = await client.PostAsJsonAsync(Results, Body() with { SchemaVersion = 3 }, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal([ErrorCodes.Benchmark.SchemaVersionUnsupported], body!.Errors);
        Assert.Empty(factory.World.Benchmarks.Items);
    }

    /// <summary>Negative: a field that breaks a domain rule is 400 with the domain code.</summary>
    [Fact]
    public async Task PostResult_InvalidField_Returns400WithTheDomainCode()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateServiceClient();
        var request = Body() with { Dataset = new BenchmarkDatasetDto("d.csv", "nothex", 10) };

        // Act
        using var response = await client.PostAsJsonAsync(Results, request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(BenchmarkErrors.DatasetHashInvalid, body!.Errors);
    }

    /// <summary>Negative: a body that is not JSON of the contract is 400.</summary>
    [Fact]
    public async Task PostResult_BodyThatIsNotTheContract_Returns400()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateServiceClient();
        using var content = new StringContent("{\"run_id\": \"not-a-guid\"}", System.Text.Encoding.UTF8, "application/json");

        // Act
        using var response = await client.PostAsync(Results, content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.World.Benchmarks.Items);
    }

    /// <summary>Equivalence: the body uses the snake_case names of the contract, as the benchmark service sends them.</summary>
    [Fact]
    public async Task PostResult_BodyWithTheContractNames_IsRead()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateServiceClient();
        const string Json = """
            {"schema_version":2,"run_id":"5b3f0e1a-0000-4000-8000-000000000001","started_at":"2026-10-19T10:00:00+00:00",
             "finished_at":"2026-10-19T10:05:00+00:00","prompt_version":"hotel-v1",
             "dataset":{"name":"d.csv","sha256":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","rows":10},
             "machine":{"cpu":"Test CPU","cpu_cores":8,"memory_mb":16384,"gpu":null,"os":"Linux 6.8"},
             "backend":{"name":"ollama","version":"0.5.1"},
             "results":[{"schema_version":2,"timestamp":"2026-10-19T10:00:00+00:00","session_id":"s1","backend":"ollama",
               "model":"llama3.2:3b","task_id":"hotel-cancellation","category":"analysis","analysis_type":"hotel_cancellation",
               "run":0,"cold":true,"ttft_s":0.5,"total_s":2.0,"completion_tokens":10,"prompt_tokens":20,"tokens_per_s":5.0,
               "tokens_estimated":false,"finish_reason":"stop","system_ram_before_mb":null,"system_ram_after_mb":null,
               "system_ram_peak_mb":1000,"gpu_vram_before_mb":null,"gpu_vram_after_mb":null,"gpu_vram_peak_mb":null,
               "passed":null,"response_valid":true,"validation_error":null,"output":"{}","error":null}]}
            """;
        using var content = new StringContent(Json, System.Text.Encoding.UTF8, "application/json");

        // Act
        using var response = await client.PostAsync(Results, content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("llama3.2:3b", Assert.Single(Assert.Single(factory.World.Benchmarks.Items).Results).Model);
    }

    #endregion

    #region Reading runs

    /// <summary>Positive: a Manager lists the runs and reads one with its requests.</summary>
    [Fact]
    public async Task GetRuns_Manager_ListsTheRunsAndReadsOne()
    {
        // Arrange
        using var factory = new ApiFactory();
        factory.World.Benchmarks.Add(NewRun());
        using var client = factory.CreateStaffClient(StaffRole.Manager);

        // Act
        var list = await client.GetFromJsonAsync<ApiResponse<List<BenchmarkRunListItemDto>>>(Runs, ApiJson.Options, TestContext.Current.CancellationToken);
        var details = await client.GetFromJsonAsync<ApiResponse<BenchmarkRunDetailsDto>>($"{Runs}/{RunId}", ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(RunId, Assert.Single(list!.Value!).RunId);
        Assert.Equal(["m1", "m2"], details!.Value!.Results.Select(result => result.Model));
    }

    /// <summary>State: the answer of a request is read on its own by the key of the request.</summary>
    [Fact]
    public async Task GetOutput_Manager_ReadsTheAnswerOfOneRequest()
    {
        // Arrange
        using var factory = new ApiFactory();
        var run = NewRun();
        factory.World.Benchmarks.Add(run);
        using var client = factory.CreateStaffClient(StaffRole.Manager);

        // Act
        var output = await client.GetFromJsonAsync<ApiResponse<BenchmarkOutputDto>>($"/api/benchmark/results/{run.Results[0].Id}/output", ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("{}", output!.Value!.Output);
    }

    /// <summary>Negative: an unknown run id is 404.</summary>
    [Fact]
    public async Task GetRun_UnknownRunId_Returns404()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateStaffClient(StaffRole.Manager);

        // Act
        using var response = await client.GetAsync($"{Runs}/{RunId}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Negative: a role without the permission is 403 on every read.</summary>
    [Theory]
    [InlineData(StaffRole.Receptionist)]
    [InlineData(StaffRole.Housekeeping)]
    public async Task Get_RoleWithoutTheBenchmarkPermission_Returns403(StaffRole role)
    {
        // Arrange
        using var factory = new ApiFactory();
        factory.World.Benchmarks.Add(NewRun());
        using var client = factory.CreateStaffClient(role);

        // Act
        using var list = await client.GetAsync(Runs, TestContext.Current.CancellationToken);
        using var one = await client.GetAsync($"{Runs}/{RunId}", TestContext.Current.CancellationToken);
        using var output = await client.GetAsync("/api/benchmark/results/1/output", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, one.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, output.StatusCode);
    }

    /// <summary>Negative: a read without a staff token is 401, even with the service token.</summary>
    [Fact]
    public async Task GetRuns_ServiceTokenOnly_Returns401()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateServiceClient();

        // Act
        using var response = await client.GetAsync(Runs, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Helpers

    private static SubmitBenchmarkRunRequest Body() =>
        new(
            2,
            RunId,
            Start,
            Start.AddMinutes(5),
            "hotel-v1",
            new BenchmarkDatasetDto("d.csv", new string('a', 64), 10),
            new BenchmarkMachineDto("Test CPU", 8, 16384, null, "Linux 6.8"),
            new BenchmarkBackendDto("ollama", "0.5.1"),
            [Result("m1"), Result("m2")]);

    private static BenchmarkRequestResultDto Result(string model) =>
        new(2, Start, "s1", "ollama", model, "hotel-cancellation", "analysis", "hotel_cancellation", 0, true, 0.5, 2.0, 10, 20, 5.0, false, "stop", null, null, 1000, null, null, null, null, true, null, "{}", null);

    private static BenchmarkRun NewRun() =>
        BenchmarkRun.Create(
            RunId,
            Start,
            Start.AddMinutes(5),
            "d.csv",
            new string('a', 64),
            10,
            "hotel-v1",
            new BenchmarkMachine("Test CPU", 8, 16384, null, "Linux 6.8"),
            "ollama",
            "0.5.1",
            new[] { "m1", "m2" }.Select(model => new BenchmarkResultData(Start, "ollama", model, BenchmarkResult.HotelCancellation, "hotel-cancellation", "analysis", 0, true, 0.5, 2.0, 10, 20, 5.0, false, "stop", null, null, 1000, null, null, null, null, true, null, "{}", null)).ToArray());

    #endregion
}
