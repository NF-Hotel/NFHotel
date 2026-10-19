using System.Net;

using NFHotel.Application.Errors;

namespace NFHotel.ApiClient.Tests;

/// <summary>The web application reads the stored benchmark runs through the API (ADR-0008); it never posts a run.</summary>
public sealed class BenchmarkRequestTests
{
    private static readonly Uri Base = new("http://api.test/");
    private static readonly Guid RunId = Guid.Parse("5b3f0e1a-0000-4000-8000-000000000001");

    #region Paths

    /// <summary>Positive: the list is a GET of the runs path.</summary>
    [Fact]
    public async Task GetRunsAsync_Call_GetsTheRunsPath()
    {
        // Arrange
        var (handler, service) = Build(HttpStatusCode.OK, """{"value":[],"errors":[]}""");

        // Act
        var result = await service.GetRunsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal((HttpMethod.Get, "/api/benchmark/runs"), (handler.Method!, handler.PathAndQuery!));
    }

    /// <summary>Positive: one run is a GET of the run id below the runs path.</summary>
    [Fact]
    public async Task GetRunAsync_RunId_GetsThePathOfThatRun()
    {
        // Arrange
        var (handler, service) = Build(HttpStatusCode.NotFound, """{"value":null,"errors":["benchmark.not_found"]}""");

        // Act
        var result = await service.GetRunAsync(RunId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((HttpMethod.Get, $"/api/benchmark/runs/{RunId}"), (handler.Method!, handler.PathAndQuery!));
        Assert.Equal(ErrorCodes.Benchmark.NotFound, Assert.Single(result.Errors).Code);
    }

    /// <summary>Positive: the answer of a request is a GET of its own path.</summary>
    [Fact]
    public async Task GetOutputAsync_Key_GetsTheOutputPath()
    {
        // Arrange
        var (handler, service) = Build(HttpStatusCode.OK, """{"value":{"benchmarkResultId":7,"model":"m1","output":"{}","validationError":null,"error":null},"errors":[]}""");

        // Act
        var result = await service.GetOutputAsync(7, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((HttpMethod.Get, "/api/benchmark/results/7/output"), (handler.Method!, handler.PathAndQuery!));
        Assert.Equal("{}", result.Value.Output);
    }

    #endregion

    #region Refusals

    /// <summary>Negative: the empty run id is not sent to the API and fails with not found.</summary>
    [Fact]
    public async Task GetRunAsync_EmptyRunId_FailsWithoutACall()
    {
        // Arrange
        var (handler, service) = Build(HttpStatusCode.OK, """{"value":null,"errors":[]}""");

        // Act
        var result = await service.GetRunAsync(Guid.Empty, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Benchmark.NotFound, Assert.Single(result.Errors).Code);
        Assert.Equal(0, handler.Calls);
    }

    /// <summary>Negative: a key below one is a programming error and throws.</summary>
    [Fact]
    public async Task GetOutputAsync_KeyBelowOne_Throws()
    {
        // Arrange
        var (_, service) = Build(HttpStatusCode.OK, """{"value":null,"errors":[]}""");

        // Act and assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.GetOutputAsync(0, TestContext.Current.CancellationToken));
    }

    #endregion

    #region Helpers

    private static (StubHandler Handler, ApiBenchmarkService Service) Build(HttpStatusCode status, string json)
    {
        var handler = StubHandler.Json(status, json);
        var gateway = new ApiGateway(new HttpClient(handler, disposeHandler: false) { BaseAddress = Base });
        return (handler, new ApiBenchmarkService(gateway));
    }

    #endregion
}
