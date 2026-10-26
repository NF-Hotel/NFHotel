using System.Net;
using System.Net.Http.Json;

using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Benchmark;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Api.Tests;

/// <summary>
/// The records of started benchmark runs (ADR-0031): a Manager records a run and its states with the permission to run a
/// benchmark, and reads the recent ones with the permission to see the runs.
/// </summary>
public sealed class BenchmarkRequestsEndpointTests
{
    private const string Route = "/api/benchmark/requests";
    private static readonly Guid RunId = Guid.Parse("5b3f0e1a-0000-4000-8000-000000000001");
    private static readonly RecordBenchmarkRequest Choice = new(RunId, ["phi4-mini:latest", "llama3.1:latest"], true, false, 1);

    /// <summary>Positive: a Manager records a run; the staff name comes from the token and the run is queued.</summary>
    [Fact]
    public async Task Post_Manager_RecordsTheRunQueued()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateStaffClient(StaffRole.Manager);

        // Act
        using var response = await client.PostAsJsonAsync(Route, Choice, ApiJson.Options, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BenchmarkRequestDto>>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal((RunId, BenchmarkRunStates.Queued), (body!.Value!.RunId, body.Value.State));
        Assert.False(string.IsNullOrWhiteSpace(body.Value.RequestedBy));
        Assert.Single(factory.World.BenchmarkRequests.Items);
    }

    /// <summary>State: an interrupted run is recorded, and the list shows it with its reason.</summary>
    [Fact]
    public async Task PutState_Interrupted_IsListed()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateStaffClient(StaffRole.Manager);
        await client.PostAsJsonAsync(Route, Choice, ApiJson.Options, TestContext.Current.CancellationToken);

        // Act
        using var response = await client.PutAsJsonAsync($"{Route}/{RunId}/state", new SetBenchmarkRequestStateRequest(BenchmarkRunStates.Interrupted, "restarted"), ApiJson.Options, TestContext.Current.CancellationToken);
        var list = await client.GetFromJsonAsync<ApiResponse<List<BenchmarkRequestDto>>>(Route, ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var listed = Assert.Single(list!.Value!);
        Assert.Equal((BenchmarkRunStates.Interrupted, "restarted"), (listed.State, listed.Error));
    }

    /// <summary>Negative: a step back from a final state is refused with its code (422).</summary>
    [Fact]
    public async Task PutState_BackFromInterrupted_Returns422()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateStaffClient(StaffRole.Manager);
        await client.PostAsJsonAsync(Route, Choice, ApiJson.Options, TestContext.Current.CancellationToken);
        await client.PutAsJsonAsync($"{Route}/{RunId}/state", new SetBenchmarkRequestStateRequest(BenchmarkRunStates.Interrupted), ApiJson.Options, TestContext.Current.CancellationToken);

        // Act
        using var response = await client.PutAsJsonAsync($"{Route}/{RunId}/state", new SetBenchmarkRequestStateRequest(BenchmarkRunStates.Running), ApiJson.Options, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<BenchmarkRequestDto>>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains(BenchmarkRequestErrors.StateChangeRefused, body!.Errors);
    }

    /// <summary>Negative: the state of a run that was never recorded is 404.</summary>
    [Fact]
    public async Task PutState_UnknownRun_Returns404()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateStaffClient(StaffRole.Manager);

        // Act
        using var response = await client.PutAsJsonAsync($"{Route}/{RunId}/state", new SetBenchmarkRequestStateRequest(BenchmarkRunStates.Running), ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Negative: a role without the permission to run a benchmark cannot record one, and nothing is stored.</summary>
    [Theory]
    [InlineData(StaffRole.Receptionist)]
    [InlineData(StaffRole.Housekeeping)]
    public async Task Post_RoleWithoutThePermission_Returns403(StaffRole role)
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateStaffClient(role);

        // Act
        using var response = await client.PostAsJsonAsync(Route, Choice, ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(factory.World.BenchmarkRequests.Items);
    }

    /// <summary>Negative: the service token alone can neither record a run nor read the list.</summary>
    [Fact]
    public async Task ServiceTokenOnly_Returns401()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateServiceClient();

        // Act
        using var post = await client.PostAsJsonAsync(Route, Choice, ApiJson.Options, TestContext.Current.CancellationToken);
        using var get = await client.GetAsync(Route, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized), (post.StatusCode, get.StatusCode));
        Assert.Empty(factory.World.BenchmarkRequests.Items);
    }
}
