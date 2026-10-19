using System.Net;

using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Errors;

namespace NFHotel.ApiClient.Tests;

/// <summary>The web application starts the market analysis and reads its report through the API (MIL-026).</summary>
public sealed class MarketAnalysisRequestTests
{
    private static readonly Uri Base = new("http://api.test/");
    private static readonly Guid RunId = Guid.Parse("5b3f0e1a-0000-4000-8000-0000000000a1");

    /// <summary>Positive: a start is a POST of the runs path.</summary>
    [Fact]
    public async Task StartRunAsync_Call_PostsTheRunsPath()
    {
        // Arrange
        var (handler, service) = Build($$$"""{"value":{"runId":"{{{RunId}}}","state":"queued","startedAt":null,"finishedAt":null,"error":null},"errors":[]}""");

        // Act
        var result = await service.StartRunAsync(new StartMarketAnalysisRequest(new DateOnly(2019, 1, 1), null), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((HttpMethod.Post, "/api/market-analysis/runs", MarketAnalysisRunStates.Queued), (handler.Method!, handler.PathAndQuery!, result.Value.State));
        Assert.Contains("\"from\":\"2019-01-01\"", handler.Body, StringComparison.Ordinal);
        Assert.Contains("\"to\":null", handler.Body, StringComparison.Ordinal);
    }

    /// <summary>Positive: the state of a run is a GET of that run below the runs path.</summary>
    [Fact]
    public async Task GetRunStateAsync_RunId_GetsThePathOfThatRun()
    {
        // Arrange
        var (handler, service) = Build($$$"""{"value":{"runId":"{{{RunId}}}","state":"running","startedAt":null,"finishedAt":null,"error":null},"errors":[]}""");

        // Act
        var result = await service.GetRunStateAsync(RunId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((HttpMethod.Get, $"/api/market-analysis/runs/{RunId}", MarketAnalysisRunStates.Running), (handler.Method!, handler.PathAndQuery!, result.Value.State));
    }

    /// <summary>Negative: the empty run id is not sent and is answered as not found.</summary>
    [Fact]
    public async Task GetRunStateAsync_EmptyId_IsNotFoundWithoutACall()
    {
        // Arrange
        var (handler, service) = Build("{}");

        // Act
        var result = await service.GetRunStateAsync(Guid.Empty, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysisRunner.RunNotFound, Assert.Single(result.Errors).Code);
        Assert.Null(handler.Method);
    }

    /// <summary>Positive: the latest report is a GET of the latest path, with the content read.</summary>
    [Fact]
    public async Task GetLatestReportAsync_Call_GetsTheLatestPathAndReadsTheContent()
    {
        // Arrange
        var (handler, service) = Build($$$"""{"value":{"runId":"{{{RunId}}}","startedAt":"2026-10-19T10:00:00+00:00","finishedAt":"2026-10-19T10:04:00+00:00","provider":"ollama","model":"m","content":{"figures":[],"sections":[{"id":"a","title":"A","status":"available","reason":null,"series":[{"id":"s","title":"S","unit":"u","points":[{"label":"x","value":1.5}]}],"insight":{"status":"available","reason":null,"executiveSummary":"es","suggestions":[]}}]}},"errors":[]}""");

        // Act
        var result = await service.GetLatestReportAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((HttpMethod.Get, "/api/market-analysis/latest"), (handler.Method!, handler.PathAndQuery!));
        var section = Assert.Single(result.Value.Content.Sections);
        Assert.Equal(1.5, Assert.Single(Assert.Single(section.Series).Points).Value);
        Assert.Equal("es", section.Insight!.ExecutiveSummary);
    }

    /// <summary>Negative: a 404 from the API keeps its code, so the page can show the empty message.</summary>
    [Fact]
    public async Task GetLatestReportAsync_NotFound_KeepsTheCode()
    {
        // Arrange
        var (_, service) = Build("""{"value":null,"errors":["market_analysis.not_found"]}""", HttpStatusCode.NotFound);

        // Act
        var result = await service.GetLatestReportAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.MarketAnalysis.NotFound, Assert.Single(result.Errors).Code);
    }

    private static (StubHandler Handler, ApiMarketAnalysisService Service) Build(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = StubHandler.Json(status, json);
        var gateway = new ApiGateway(new HttpClient(handler, disposeHandler: false) { BaseAddress = Base });
        return (handler, new ApiMarketAnalysisService(gateway));
    }
}
