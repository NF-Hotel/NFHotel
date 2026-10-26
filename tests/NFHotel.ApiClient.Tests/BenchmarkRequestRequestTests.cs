using System.Net;

using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Errors;

namespace NFHotel.ApiClient.Tests;

/// <summary>The web application records the started benchmark runs and their states through the API (ADR-0031).</summary>
public sealed class BenchmarkRequestRequestTests
{
    private static readonly Uri Base = new("http://api.test/");
    private static readonly Guid RunId = Guid.Parse("5b3f0e1a-0000-4000-8000-000000000001");
    private const string RecordJson = """{"value":{"runId":"5b3f0e1a-0000-4000-8000-000000000001","models":["m1"],"hotelAnalysis":true,"deterministicTasks":false,"runs":1,"requestedBy":"mona","requestedAt":"2026-10-26T00:09:00+00:00","state":"queued","error":null,"updatedAt":"2026-10-26T00:09:00+00:00"},"errors":[]}""";

    /// <summary>Positive: recording a run is a POST of the requests path.</summary>
    [Fact]
    public async Task RecordAsync_Call_PostsTheRequestsPath()
    {
        // Arrange
        var (handler, service) = Build(RecordJson);

        // Act
        var result = await service.RecordAsync(new RecordBenchmarkRequest(RunId, ["m1"], true, false, 1), string.Empty, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(BenchmarkRunStates.Queued, result.Value.State);
        Assert.Equal((HttpMethod.Post, "/api/benchmark/requests"), (handler.Method!, handler.PathAndQuery!));
    }

    /// <summary>Positive: a state is a PUT below the run.</summary>
    [Fact]
    public async Task SetStateAsync_Run_PutsThePathOfItsState()
    {
        // Arrange
        var (handler, service) = Build(RecordJson);

        // Act
        await service.SetStateAsync(RunId, new SetBenchmarkRequestStateRequest(BenchmarkRunStates.Interrupted), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((HttpMethod.Put, $"/api/benchmark/requests/{RunId}/state"), (handler.Method!, handler.PathAndQuery!));
    }

    /// <summary>Negative: an empty run id is not found without a call.</summary>
    [Fact]
    public async Task SetStateAsync_EmptyRunId_IsNotFoundWithoutACall()
    {
        // Arrange
        var (handler, service) = Build(RecordJson);

        // Act
        var result = await service.SetStateAsync(Guid.Empty, new SetBenchmarkRequestStateRequest(BenchmarkRunStates.Running), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Benchmark.NotFound, Assert.Single(result.Errors).Code);
        Assert.Null(handler.Method);
    }

    /// <summary>Positive: the list is a GET of the requests path.</summary>
    [Fact]
    public async Task GetRecentAsync_Call_GetsTheRequestsPath()
    {
        // Arrange
        var (handler, service) = Build("""{"value":[],"errors":[]}""");

        // Act
        var result = await service.GetRecentAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result.Value);
        Assert.Equal((HttpMethod.Get, "/api/benchmark/requests"), (handler.Method!, handler.PathAndQuery!));
    }

    private static (StubHandler Handler, ApiBenchmarkRequestService Service) Build(string json)
    {
        var handler = StubHandler.Json(HttpStatusCode.OK, json);
        var gateway = new ApiGateway(new HttpClient(handler, disposeHandler: false) { BaseAddress = Base });
        return (handler, new ApiBenchmarkRequestService(gateway));
    }
}
