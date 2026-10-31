using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.MarketAnalysis;
using NFHotel.Web.Common;
using NFHotel.Web.Features.Admin.Shared;

namespace NFHotel.Web.Features.Admin.MarketAnalysis;

/// <summary>
/// State of the page where a Manager reads the market analysis report, starts a new run and follows it until its result
/// is stored (MIL-026). The page holds only markup and binding.
/// </summary>
public sealed class MarketAnalysisViewModel
{
    private readonly IUseCaseRunner<IMarketAnalysisService> _service;
    private readonly IClock _clock;

    /// <summary>Creates the view model.</summary>
    /// <param name="service">Runs the calls to the market analysis use cases.</param>
    /// <param name="clock">The clock of the hotel, for the quick choices of the period.</param>
    public MarketAnalysisViewModel(IUseCaseRunner<IMarketAnalysisService> service, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(clock);

        _service = service;
        _clock = clock;
    }

    /// <summary>The report that was stored last, or <c>null</c> when none is stored yet.</summary>
    public MarketAnalysisReportDto? Report { get; private set; }

    /// <summary>The run that was started and is followed, or <c>null</c>.</summary>
    public MarketAnalysisRunStateDto? CurrentRun { get; private set; }

    /// <summary>The first arrival date of the bookings to analyse, or <c>null</c> for no lower limit.</summary>
    public DateOnly? From { get; set; }

    /// <summary>The last arrival date of the bookings to analyse, or <c>null</c> for no upper limit.</summary>
    public DateOnly? To { get; set; }

    /// <summary>Fills the dates with the last years up to today, for example the last 3 years.</summary>
    /// <param name="years">The number of years; at least 1.</param>
    public void UseLastYears(int years)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(years, 1);

        DateOnly today = _clock.Today;
        From = today.AddYears(-years);
        To = today;
    }

    /// <summary>Clears both dates, so that every booking is analysed.</summary>
    public void UseAllBookings()
    {
        From = null;
        To = null;
    }

    /// <summary>The texts of the last refusal, empty when the last call succeeded.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = [];

    /// <summary>True while a call is running.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>True once the report has been asked for, with or without an answer.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>True when the report was read and no run has been stored yet, so the page says so.</summary>
    public bool HasNoReport => IsLoaded && Report is null && Errors.Count == 0;

    /// <summary>True while the page should keep asking for the state of the run.</summary>
    public bool IsFollowing => CurrentRun is { IsFinished: false };

    /// <summary>True when the choice to start is open: no call is running and no run is followed.</summary>
    public bool CanStart => !IsBusy && !IsFollowing;

    /// <summary>True when the last followed run failed, so the page shows its reason.</summary>
    public bool LastRunFailed => CurrentRun is { State: MarketAnalysisRunStates.Failed };

    /// <summary>The text of the reason the last followed run failed, or <c>null</c> when it did not fail.</summary>
    public string? FailureText =>
        CurrentRun is { State: MarketAnalysisRunStates.Failed } run
            ? ErrorMessages.Describe(new Error(run.Error ?? ErrorCodes.MarketAnalysisRunner.UnexpectedResponse))
            : null;

    /// <summary>The analyses of the report as the page draws them: the charts of each, with the kind the rule chose, and its AI insight.</summary>
    public IReadOnlyList<MarketAnalysisSectionView> Sections =>
        Report is null
            ? []
            : [.. Report.Content.Sections.Select(section => new MarketAnalysisSectionView(
                section,
                [.. section.Series
                    .Where(series => series.Points.Count > 0)
                    .Select(series => new MarketAnalysisChart(series, ChartKindSelector.Select(series)))]))];

    /// <summary>Loads the stored report when the page opens.</summary>
    /// <param name="cancellationToken">Cancels the call when the page is closed.</param>
    /// <returns>A task that ends when the report is loaded.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        try
        {
            await LoadReportAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
            IsLoaded = true;
        }
    }

    /// <summary>Starts a run of the market analysis.</summary>
    /// <param name="cancellationToken">Cancels the call when the page is closed.</param>
    /// <returns>A task that ends when the service has answered.</returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!CanStart)
        {
            return;
        }

        StartMarketAnalysisRequest request = new(From, To);

        IsBusy = true;
        try
        {
            Result<MarketAnalysisRunStateDto> result = await _service
                .RunAsync((service, token) => service.StartRunAsync(request, token), cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                return;
            }

            Errors = [];
            CurrentRun = result.Value;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Asks for the state of the run that is followed. When the run has completed the API has stored its result, so the
    /// report is read again. A run that the service has forgotten ends the following.
    /// </summary>
    /// <param name="cancellationToken">Cancels the call when the page is closed.</param>
    /// <returns>A task that ends when the service has answered.</returns>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentRun is not { } run)
        {
            return;
        }

        Result<MarketAnalysisRunStateDto> result = await _service
            .RunAsync((service, token) => service.GetRunStateAsync(run.RunId, token), cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            Errors = ErrorMessages.Describe(result);
            if (result.Errors.Any(error => error.Code == ErrorCodes.MarketAnalysisRunner.RunNotFound))
            {
                // The service deleted the run: there is nothing more to follow.
                CurrentRun = null;
            }

            return;
        }

        Errors = [];
        CurrentRun = result.Value;

        if (result.Value.State == MarketAnalysisRunStates.Completed)
        {
            await LoadReportAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task LoadReportAsync(CancellationToken cancellationToken)
    {
        Result<MarketAnalysisReportDto> result = await _service
            .RunAsync((service, token) => service.GetLatestReportAsync(token), cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            Errors = [];
            Report = result.Value;
            return;
        }

        // No stored run is not a fault: the page shows the empty message instead of an error.
        if (result.Errors.Any(error => error.Code == ErrorCodes.MarketAnalysis.NotFound))
        {
            Errors = [];
            Report = null;
            return;
        }

        Errors = ErrorMessages.Describe(result);
    }
}

/// <summary>One analysis of the report as the page draws it.</summary>
/// <param name="Section">The analysis with its AI insight.</param>
/// <param name="Charts">One chart for each series that has points.</param>
public sealed record MarketAnalysisSectionView(MarketAnalysisSectionDto Section, IReadOnlyList<MarketAnalysisChart> Charts)
{
    /// <summary>The correlation matrices of the analysis, drawn as heat maps.</summary>
    public IReadOnlyList<MarketAnalysisMatrixDto> Matrices => Section.Matrices ?? [];

    /// <summary>True when the analysis has something to draw.</summary>
    public bool HasCharts => Charts.Count > 0 || Matrices.Count > 0;
}

/// <summary>One chart of an analysis as the page draws it.</summary>
/// <param name="Series">The series to draw.</param>
/// <param name="Kind">The kind of chart that the rule of <see cref="ChartKindSelector"/> chose for the series.</param>
public sealed record MarketAnalysisChart(MarketAnalysisSeriesDto Series, ChartKind Kind);
