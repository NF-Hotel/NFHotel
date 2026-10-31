using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.MarketAnalysis;
using NFHotel.TestSupport;
using NFHotel.Web.Common;
using NFHotel.Web.Features.Admin.MarketAnalysis;
using NFHotel.Web.Features.Admin.Shared;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>Reading the market analysis report, starting a run and following it until the report is stored (MIL-026).</summary>
public sealed class MarketAnalysisViewModelTests
{
    private static readonly Guid RunId = Guid.Parse("5b3f0e1a-0000-4000-8000-0000000000a1");

    #region Report

    /// <summary>Positive: the stored report is loaded with each analysis, its charts and its AI insight.</summary>
    [Fact]
    public async Task InitializeAsync_ReportStored_LoadsTheSectionsWithChartsAndInsights()
    {
        // Arrange
        MarketAnalysisViewModel viewModel = Build(out _);

        // Act
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        MarketAnalysisSectionView cancellations = viewModel.Sections[0];
        MarketAnalysisChart chart = Assert.Single(cancellations.Charts);
        Assert.Equal(["Cancellations", "Room value and stay"], viewModel.Sections.Select(view => view.Section.Title));
        Assert.Equal("By deposit type", chart.Series.Title);
        Assert.Equal(["No Deposit", "Non Refund"], chart.Series.Points.Select(point => point.Label));
        Assert.Equal(27.5, chart.Series.Points[0].Value);
        Assert.Equal(ChartKind.Columns, chart.Kind);
        Assert.Equal("Non-refundable bookings are cancelled most often.", cancellations.Section.Insight!.ExecutiveSummary);
        Assert.Empty(viewModel.Sections[1].Charts);
        Assert.False(viewModel.HasNoReport);
    }

    /// <summary>Negative: with no stored run the page says so and shows no error.</summary>
    [Fact]
    public async Task InitializeAsync_NothingStored_ShowsTheEmptyMessageAndNoError()
    {
        // Arrange
        MarketAnalysisViewModel viewModel = Build(out IMarketAnalysisService? service);
        service.GetLatestReportAsync(Arg.Any<CancellationToken>()).Returns(Result<MarketAnalysisReportDto>.Failure(ErrorCodes.MarketAnalysis.NotFound));

        // Act
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(viewModel.HasNoReport);
        Assert.Empty(viewModel.Errors);
        Assert.Empty(viewModel.Sections);
    }

    /// <summary>Negative: an API that cannot be used shows the text and is not the empty message.</summary>
    [Fact]
    public async Task InitializeAsync_ApiFails_ShowsTheTextAndNotTheEmptyMessage()
    {
        // Arrange
        MarketAnalysisViewModel viewModel = Build(out IMarketAnalysisService? service);
        service.GetLatestReportAsync(Arg.Any<CancellationToken>()).Returns(Result<MarketAnalysisReportDto>.Failure(ErrorCodes.Api.Unavailable));

        // Act
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("We couldn't reach the hotel system. Please try again in a moment.", Assert.Single(viewModel.Errors));
        Assert.False(viewModel.HasNoReport);
        Assert.True(viewModel.IsLoaded);
    }

    /// <summary>Boundary: a series without points is not drawn.</summary>
    [Fact]
    public async Task Sections_SeriesWithoutPoints_AreLeftOut()
    {
        // Arrange
        MarketAnalysisViewModel viewModel = Build(out IMarketAnalysisService? service);
        MarketAnalysisReportDto report = Report() with
        {
            Content = new MarketAnalysisContentDto([], [new MarketAnalysisSectionDto("x", "X", "available", null, [new MarketAnalysisSeriesDto("empty", "Empty", "", [])], null)]),
        };
        service.GetLatestReportAsync(Arg.Any<CancellationToken>()).Returns(Result<MarketAnalysisReportDto>.Success(report));

        // Act
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(Assert.Single(viewModel.Sections).Charts);
        Assert.NotNull(viewModel.Report);
    }

    #endregion

    #region Run

    /// <summary>Positive: a start follows the queued run and blocks a second start.</summary>
    [Fact]
    public async Task StartAsync_ServiceUp_FollowsTheQueuedRunAndBlocksASecondStart()
    {
        // Arrange
        MarketAnalysisViewModel viewModel = Build(out IMarketAnalysisService? service);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);

        // Act
        await viewModel.StartAsync(TestContext.Current.CancellationToken);
        await viewModel.StartAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(viewModel.IsFollowing);
        Assert.False(viewModel.CanStart);
        await service.Received(1).StartRunAsync(Arg.Any<StartMarketAnalysisRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Positive: the dates chosen on the page are the period of the start; no date is an open period.</summary>
    [Fact]
    public async Task StartAsync_DatesChosen_SendsThePeriod()
    {
        // Arrange
        MarketAnalysisViewModel viewModel = Build(out IMarketAnalysisService? service);
        viewModel.From = new DateOnly(2019, 1, 1);
        viewModel.To = new DateOnly(2019, 12, 31);

        // Act
        await viewModel.StartAsync(TestContext.Current.CancellationToken);

        // Assert
        await service.Received(1).StartRunAsync(new StartMarketAnalysisRequest(new DateOnly(2019, 1, 1), new DateOnly(2019, 12, 31)), Arg.Any<CancellationToken>());
    }

    /// <summary>Positive: the quick choice of the last 3 years fills the dates from three years ago to today, and the start sends them.</summary>
    [Fact]
    public async Task UseLastYears_ThreeYears_FillsTheDatesAndTheStartSendsThem()
    {
        // Arrange
        MarketAnalysisViewModel viewModel = Build(out IMarketAnalysisService? service);

        // Act
        viewModel.UseLastYears(3);
        await viewModel.StartAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((new DateOnly(2023, 10, 20), new DateOnly(2026, 10, 20)), (viewModel.From, viewModel.To));
        await service.Received(1).StartRunAsync(new StartMarketAnalysisRequest(new DateOnly(2023, 10, 20), new DateOnly(2026, 10, 20)), Arg.Any<CancellationToken>());
    }

    /// <summary>Boundary: one year gives the same day one year back.</summary>
    [Fact]
    public void UseLastYears_OneYear_GivesOneYearBack()
    {
        // Arrange
        MarketAnalysisViewModel viewModel = Build(out _);

        // Act
        viewModel.UseLastYears(1);

        // Assert
        Assert.Equal(new DateOnly(2025, 10, 20), viewModel.From);
    }

    /// <summary>Negative: fewer than one year is refused, and the dates are not changed.</summary>
    [Fact]
    public void UseLastYears_Zero_IsRefusedAndKeepsTheDates()
    {
        // Arrange
        MarketAnalysisViewModel viewModel = Build(out _);
        viewModel.From = new DateOnly(2019, 1, 1);

        // Act and assert
        Assert.Throws<ArgumentOutOfRangeException>(() => viewModel.UseLastYears(0));
        Assert.Equal(new DateOnly(2019, 1, 1), viewModel.From);
    }

    /// <summary>Positive: all bookings clears both dates.</summary>
    [Fact]
    public void UseAllBookings_DatesChosen_ClearsThem()
    {
        // Arrange
        MarketAnalysisViewModel viewModel = Build(out _);
        viewModel.UseLastYears(3);

        // Act
        viewModel.UseAllBookings();

        // Assert
        Assert.Equal((null, null), (viewModel.From, viewModel.To));
    }

    /// <summary>Boundary: without dates the start asks for every booking.</summary>
    [Fact]
    public async Task StartAsync_NoDates_SendsAnOpenPeriod()
    {
        // Arrange
        MarketAnalysisViewModel viewModel = Build(out IMarketAnalysisService? service);

        // Act
        await viewModel.StartAsync(TestContext.Current.CancellationToken);

        // Assert
        await service.Received(1).StartRunAsync(new StartMarketAnalysisRequest(), Arg.Any<CancellationToken>());
    }

    /// <summary>Negative: a refused start shows the text and starts nothing to follow.</summary>
    [Theory]
    [InlineData("market_analysis_runner.already_running", "An analysis is already running. Wait until it is done.")]
    [InlineData("market_analysis_runner.unavailable", "We couldn't reach the market analysis service, or it took too long. Please try again.")]
    [InlineData("market_analysis_runner.not_configured", "The market analysis service is not set up. Please tell the administrator.")]
    public async Task StartAsync_Refused_ShowsTheTextAndFollowsNothing(string code, string text)
    {
        // Arrange
        MarketAnalysisViewModel viewModel = Build(out IMarketAnalysisService? service);
        service.StartRunAsync(Arg.Any<StartMarketAnalysisRequest>(), Arg.Any<CancellationToken>()).Returns(Result<MarketAnalysisRunStateDto>.Failure(code));

        // Act
        await viewModel.StartAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(text, Assert.Single(viewModel.Errors));
        Assert.Null(viewModel.CurrentRun);
        Assert.True(viewModel.CanStart);
    }

    /// <summary>State: the followed run goes from queued to running to completed, and the completed state loads the new report.</summary>
    [Fact]
    public async Task RefreshAsync_RunCompletes_LoadsTheNewReportAndStopsFollowing()
    {
        // Arrange
        MarketAnalysisViewModel viewModel = Build(out IMarketAnalysisService? service);
        service.GetLatestReportAsync(Arg.Any<CancellationToken>()).Returns(
            Result<MarketAnalysisReportDto>.Failure(ErrorCodes.MarketAnalysis.NotFound),
            Result<MarketAnalysisReportDto>.Success(Report()));
        service.GetRunStateAsync(RunId, Arg.Any<CancellationToken>()).Returns(
            Result<MarketAnalysisRunStateDto>.Success(State(MarketAnalysisRunStates.Running)),
            Result<MarketAnalysisRunStateDto>.Success(State(MarketAnalysisRunStates.Completed)));
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        await viewModel.StartAsync(TestContext.Current.CancellationToken);

        // Act
        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);
        MarketAnalysisReportDto? runningReport = viewModel.Report;
        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(runningReport);
        Assert.NotNull(viewModel.Report);
        Assert.False(viewModel.IsFollowing);
        Assert.True(viewModel.CanStart);
    }

    /// <summary>Negative: a failed run stops the following and shows its reason.</summary>
    [Fact]
    public async Task RefreshAsync_RunFails_StopsFollowingAndKeepsTheOldReport()
    {
        // Arrange
        MarketAnalysisViewModel viewModel = Build(out IMarketAnalysisService? service);
        service.GetRunStateAsync(RunId, Arg.Any<CancellationToken>()).Returns(
            Result<MarketAnalysisRunStateDto>.Success(new(RunId, MarketAnalysisRunStates.Failed, null, null, "market_analysis_runner.unexpected_response")));
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        await viewModel.StartAsync(TestContext.Current.CancellationToken);

        // Act
        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(viewModel.LastRunFailed);
        Assert.Equal("The market analysis service answered something we couldn't use. Please try again.", viewModel.FailureText);
        Assert.False(viewModel.IsFollowing);
        Assert.NotNull(viewModel.Report);
    }

    /// <summary>Positive: a run that failed in the service shows the error code of the service, and a failed run without one shows the plain text.</summary>
    [Theory]
    [InlineData("market_analysis_runner.run_failed:NO_DATA", "The market analysis run failed in the service (NO_DATA).")]
    [InlineData("market_analysis_runner.run_failed", "The market analysis run failed in the service.")]
    [InlineData("market_analysis_runner.run_failed:", "Something went wrong. Please try again.")]
    public async Task FailureText_RunFailedInTheService_ShowsTheServiceCode(string code, string text)
    {
        // Arrange
        MarketAnalysisViewModel viewModel = Build(out IMarketAnalysisService? service);
        service.GetRunStateAsync(RunId, Arg.Any<CancellationToken>()).Returns(
            Result<MarketAnalysisRunStateDto>.Success(new(RunId, MarketAnalysisRunStates.Failed, null, null, code)));
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        await viewModel.StartAsync(TestContext.Current.CancellationToken);

        // Act
        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(text, viewModel.FailureText);
    }

    /// <summary>Negative: a run that the service has forgotten ends the following with the text.</summary>
    [Fact]
    public async Task RefreshAsync_RunNotFound_EndsTheFollowing()
    {
        // Arrange
        MarketAnalysisViewModel viewModel = Build(out IMarketAnalysisService? service);
        service.GetRunStateAsync(RunId, Arg.Any<CancellationToken>()).Returns(Result<MarketAnalysisRunStateDto>.Failure(ErrorCodes.MarketAnalysisRunner.RunNotFound));
        await viewModel.StartAsync(TestContext.Current.CancellationToken);

        // Act
        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(viewModel.CurrentRun);
        Assert.StartsWith("The API no longer knows this run", Assert.Single(viewModel.Errors), StringComparison.Ordinal);
    }

    /// <summary>Negative: a refresh with no run followed asks the service for nothing.</summary>
    [Fact]
    public async Task RefreshAsync_NoRun_AsksForNothing()
    {
        // Arrange
        MarketAnalysisViewModel viewModel = Build(out IMarketAnalysisService? service);

        // Act
        await viewModel.RefreshAsync(TestContext.Current.CancellationToken);

        // Assert
        await service.DidNotReceive().GetRunStateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helpers

    private static MarketAnalysisRunStateDto State(string state) => new(RunId, state, null, null, null);

    private static MarketAnalysisReportDto Report()
    {
        MarketAnalysisResultDto sample = FakeMarketAnalysisServiceClient.SampleResult();
        return new(sample.RunId, sample.StartedAt, sample.FinishedAt, sample.Provider, sample.Model, sample.Content!);
    }

    private static MarketAnalysisViewModel Build(out IMarketAnalysisService service)
    {
        IMarketAnalysisService fake = Substitute.For<IMarketAnalysisService>();
        fake.GetLatestReportAsync(Arg.Any<CancellationToken>()).Returns(Result<MarketAnalysisReportDto>.Success(Report()));
        fake.StartRunAsync(Arg.Any<StartMarketAnalysisRequest>(), Arg.Any<CancellationToken>()).Returns(Result<MarketAnalysisRunStateDto>.Success(State(MarketAnalysisRunStates.Queued)));
        fake.GetRunStateAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Result<MarketAnalysisRunStateDto>.Success(State(MarketAnalysisRunStates.Running)));
        service = fake;

        IUseCaseRunner<IMarketAnalysisService> runner = Substitute.For<IUseCaseRunner<IMarketAnalysisService>>();
        runner.RunAsync(
                Arg.Any<Func<IMarketAnalysisService, CancellationToken, Task<Result<MarketAnalysisReportDto>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<IMarketAnalysisService, CancellationToken, Task<Result<MarketAnalysisReportDto>>>>()(fake, call.Arg<CancellationToken>()));
        runner.RunAsync(
                Arg.Any<Func<IMarketAnalysisService, CancellationToken, Task<Result<MarketAnalysisRunStateDto>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<IMarketAnalysisService, CancellationToken, Task<Result<MarketAnalysisRunStateDto>>>>()(fake, call.Arg<CancellationToken>()));
        return new MarketAnalysisViewModel(runner, new FakeClock { Today = new DateOnly(2026, 10, 20) });
    }

    #endregion
}
