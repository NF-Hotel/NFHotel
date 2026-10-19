using System.Net;
using System.Net.Http.Json;

using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Api.Tests;

/// <summary>
/// The market analysis endpoints (MIL-026): only a Manager starts a run or reads the report; a start returns at once and
/// the work runs in the background.
/// </summary>
public sealed class MarketAnalysisEndpointTests
{
    private const string Route = "/api/market-analysis";

    #region Manager

    /// <summary>Positive: a Manager starts a run; the answer is the queued run and the work is launched, not done.</summary>
    [Fact]
    public async Task PostRun_Manager_ReturnsTheQueuedRunAndLaunchesTheWork()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateStaffClient(StaffRole.Manager);

        // Act
        using var response = await client.PostAsync($"{Route}/runs", null, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<MarketAnalysisRunStateDto>>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(MarketAnalysisRunStates.Queued, body!.Value!.State);
        Assert.Equal([body.Value.RunId], factory.World.MarketAnalysisLauncher.Launched);
        Assert.Equal(0, factory.World.MarketAnalysisClient.Calls);
    }

    /// <summary>Negative: a second start while a run is active is a 409 with its code.</summary>
    [Fact]
    public async Task PostRun_RunActive_Returns409()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateStaffClient(StaffRole.Manager);
        await client.PostAsync($"{Route}/runs", null, TestContext.Current.CancellationToken);

        // Act
        using var response = await client.PostAsync($"{Route}/runs", null, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains(ErrorCodes.MarketAnalysisRunner.AlreadyRunning, body!.Errors);
        Assert.Single(factory.World.MarketAnalysisLauncher.Launched);
    }

    /// <summary>State: the state of the run follows the work, and the latest report carries the result when it is done.</summary>
    [Fact]
    public async Task GetRunThenLatest_AfterTheWork_ServesTheStateAndTheReport()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateStaffClient(StaffRole.Manager);
        var started = await (await client.PostAsync($"{Route}/runs", null, TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<ApiResponse<MarketAnalysisRunStateDto>>(ApiJson.Options, TestContext.Current.CancellationToken);
        var runId = started!.Value!.RunId;

        // Act
        var before = await client.GetFromJsonAsync<ApiResponse<MarketAnalysisRunStateDto>>($"{Route}/runs/{runId}", ApiJson.Options, TestContext.Current.CancellationToken);
        await factory.World.MarketAnalysisRunner().ExecuteAsync(runId, TestContext.Current.CancellationToken);
        var after = await client.GetFromJsonAsync<ApiResponse<MarketAnalysisRunStateDto>>($"{Route}/runs/{runId}", ApiJson.Options, TestContext.Current.CancellationToken);
        var report = await client.GetFromJsonAsync<ApiResponse<MarketAnalysisReportDto>>($"{Route}/latest", ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((MarketAnalysisRunStates.Queued, MarketAnalysisRunStates.Completed), (before!.Value!.State, after!.Value!.State));
        Assert.Equal("phi3.5:latest", report!.Value!.Model);
        Assert.Equal("Non-refundable bookings are cancelled most often.", report.Value.Content.Sections[0].Insight!.ExecutiveSummary);
    }

    /// <summary>Negative: with nothing stored the latest report is a 404 with the not found code.</summary>
    [Fact]
    public async Task GetLatest_NothingStored_Returns404()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateStaffClient(StaffRole.Manager);

        // Act
        using var response = await client.GetAsync($"{Route}/latest", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains(ErrorCodes.MarketAnalysis.NotFound, body!.Errors);
    }

    /// <summary>Negative: a run the API does not know, for example after a restart, is a 404.</summary>
    [Fact]
    public async Task GetRun_UnknownRun_Returns404()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateStaffClient(StaffRole.Manager);

        // Act
        using var response = await client.GetAsync($"{Route}/runs/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Other roles and callers

    /// <summary>Negative: a Receptionist and a Housekeeping user get 403 on all three endpoints and nothing is started.</summary>
    [Theory]
    [InlineData(StaffRole.Receptionist)]
    [InlineData(StaffRole.Housekeeping)]
    public async Task AllEndpoints_OtherRoles_Return403(StaffRole role)
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateStaffClient(role);

        // Act
        using var start = await client.PostAsync($"{Route}/runs", null, TestContext.Current.CancellationToken);
        using var state = await client.GetAsync($"{Route}/runs/{Guid.NewGuid()}", TestContext.Current.CancellationToken);
        using var latest = await client.GetAsync($"{Route}/latest", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([HttpStatusCode.Forbidden, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden], [start.StatusCode, state.StatusCode, latest.StatusCode]);
        Assert.Empty(factory.World.MarketAnalysisLauncher.Launched);
    }

    /// <summary>Negative: the service token alone, without a staff token, is a 401.</summary>
    [Fact]
    public async Task AllEndpoints_NoStaffToken_Return401()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateServiceClient();

        // Act
        using var start = await client.PostAsync($"{Route}/runs", null, TestContext.Current.CancellationToken);
        using var latest = await client.GetAsync($"{Route}/latest", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized], [start.StatusCode, latest.StatusCode]);
    }

    #endregion
}
