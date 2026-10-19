using System.Net;
using System.Net.Http.Json;

using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Errors;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

namespace NFHotel.Api.Tests;

/// <summary>
/// The market analysis endpoints (MIL-026): only a Manager starts a run or reads the report; the API stores the result of
/// a completed run when the state is read.
/// </summary>
public sealed class MarketAnalysisEndpointTests
{
    private const string Route = "/api/market-analysis";
    private static readonly Guid RunId = FakeMarketAnalysisServiceClient.RunId;

    #region Manager

    /// <summary>Positive: a Manager starts a run and the service receives the start.</summary>
    [Fact]
    public async Task PostRun_Manager_ReturnsTheQueuedRun()
    {
        // Arrange
        using var factory = new ApiFactory();
        using var client = factory.CreateStaffClient(StaffRole.Manager);

        // Act
        using var response = await client.PostAsync($"{Route}/runs", null, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<MarketAnalysisRunStateDto>>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal((RunId, MarketAnalysisRunStates.Queued), (body!.Value!.RunId, body.Value.State));
        Assert.Equal(1, factory.World.MarketAnalysisClient.StartCount);
    }

    /// <summary>Negative: a run that is already active is a 409 with its code.</summary>
    [Fact]
    public async Task PostRun_AlreadyRunning_Returns409()
    {
        // Arrange
        using var factory = new ApiFactory();
        factory.World.MarketAnalysisClient.FailWith = ErrorCodes.MarketAnalysisRunner.AlreadyRunning;
        using var client = factory.CreateStaffClient(StaffRole.Manager);

        // Act
        using var response = await client.PostAsync($"{Route}/runs", null, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains(ErrorCodes.MarketAnalysisRunner.AlreadyRunning, body!.Errors);
    }

    /// <summary>Negative: a service that is down or not set up is a 503, and one that limits the rate is a 429.</summary>
    [Theory]
    [InlineData("market_analysis_runner.unavailable", HttpStatusCode.ServiceUnavailable)]
    [InlineData("market_analysis_runner.not_configured", HttpStatusCode.ServiceUnavailable)]
    [InlineData("market_analysis_runner.rate_limited", HttpStatusCode.TooManyRequests)]
    public async Task PostRun_ServiceFails_ReturnsTheMatchingStatus(string code, HttpStatusCode status)
    {
        // Arrange
        using var factory = new ApiFactory();
        factory.World.MarketAnalysisClient.FailWith = code;
        using var client = factory.CreateStaffClient(StaffRole.Manager);

        // Act
        using var response = await client.PostAsync($"{Route}/runs", null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(status, response.StatusCode);
    }

    /// <summary>State: reading a completed run stores it, and the latest report then carries its content.</summary>
    [Fact]
    public async Task GetRunThenLatest_CompletedRun_StoresItAndServesTheReport()
    {
        // Arrange
        using var factory = new ApiFactory();
        factory.World.MarketAnalysisClient.State = MarketAnalysisRunStates.Completed;
        using var client = factory.CreateStaffClient(StaffRole.Manager);

        // Act
        using var state = await client.GetAsync($"{Route}/runs/{RunId}", TestContext.Current.CancellationToken);
        var report = await client.GetFromJsonAsync<ApiResponse<MarketAnalysisReportDto>>($"{Route}/latest", ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, state.StatusCode);
        Assert.Single(factory.World.MarketAnalysisRuns.Items);
        Assert.Equal("phi3.5:latest", report!.Value!.Model);
        Assert.Equal("Bookings by country", Assert.Single(report.Value.Content.Series).Title);
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

    /// <summary>Negative: a run the service does not know is a 404.</summary>
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

    /// <summary>Negative: a Receptionist and a Housekeeping user get 403 on all three endpoints and the service is not called.</summary>
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
        using var state = await client.GetAsync($"{Route}/runs/{RunId}", TestContext.Current.CancellationToken);
        using var latest = await client.GetAsync($"{Route}/latest", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([HttpStatusCode.Forbidden, HttpStatusCode.Forbidden, HttpStatusCode.Forbidden], [start.StatusCode, state.StatusCode, latest.StatusCode]);
        Assert.Equal(0, factory.World.MarketAnalysisClient.StartCount);
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
