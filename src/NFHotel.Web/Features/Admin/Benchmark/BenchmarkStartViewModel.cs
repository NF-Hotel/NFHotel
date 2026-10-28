using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Benchmark;

/// <summary>
/// State of the page where a Manager starts a benchmark run and follows it until its result is stored (MIL-020). The
/// page holds only markup and binding; the limits here are the same as those of the benchmark service, and the service
/// checks them again. Every run is recorded through the API, so the page picks up a run that is still going when it
/// opens, and shows a run that a restart of the service stopped (ADR-0031).
/// </summary>
public sealed class BenchmarkStartViewModel
{
    /// <summary>The most models in one run.</summary>
    public const int MaxModels = 4;

    /// <summary>The most repetitions per model and task.</summary>
    public const int MaxRuns = 5;

    private readonly IUseCaseRunner<IBenchmarkServiceClient> _client;
    private readonly BenchmarkRunTracker _tracker;
    private readonly List<string> _selected = [];

    /// <summary>Creates the view model.</summary>
    /// <param name="client">Runs the calls to the benchmark service.</param>
    /// <param name="tracker">Records the runs and their states through the API.</param>
    public BenchmarkStartViewModel(IUseCaseRunner<IBenchmarkServiceClient> client, BenchmarkRunTracker tracker)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(tracker);

        _client = client;
        _tracker = tracker;
    }

    /// <summary>The installed models that can be chosen.</summary>
    public IReadOnlyList<string> Models { get; private set; } = [];

    /// <summary>The chosen models, in the order they were chosen.</summary>
    public IReadOnlyList<string> SelectedModels => _selected;

    /// <summary>True to make the hotel cancellation analysis.</summary>
    public bool HotelAnalysis { get; set; } = true;

    /// <summary>True to make the deterministic tasks.</summary>
    public bool DeterministicTasks { get; set; }

    /// <summary>Requests per model and task, from 1 to <see cref="MaxRuns"/>.</summary>
    public int Runs { get; set; } = 1;

    /// <summary>
    /// The runs that the benchmark service knows, newest first: queued, running and the recent finished ones. They come from
    /// the service, so they are the same after a refresh of the page and for every Manager.
    /// </summary>
    public IReadOnlyList<BenchmarkRunStateDto> KnownRuns { get; private set; } = [];

    /// <summary>False when the last read of the live state failed, so the page can say that it is paused.</summary>
    public bool IsLive { get; private set; } = true;

    /// <summary>The run that was started and is shown, or <c>null</c>.</summary>
    public BenchmarkRunStateDto? CurrentRun { get; private set; }

    /// <summary>
    /// The last run that ended without a result, failed or interrupted by a restart of the service, when no run is being
    /// followed; its choice can be taken again with <see cref="UseChoiceOf"/>. <c>null</c> otherwise.
    /// </summary>
    public BenchmarkRequestDto? StoppedRun { get; private set; }

    /// <summary>The texts of the last refusal, empty when the last call succeeded.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = [];

    /// <summary>True while a call is running.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>True once the models have been asked for, with or without an answer.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>True while the page should keep asking for the state of the run.</summary>
    public bool IsFollowing =>
        CurrentRun is { } run
        && (!run.IsFinished
            || (run.State == BenchmarkRunStates.Done && run.Delivery == BenchmarkDeliveryStates.Pending));

    /// <summary>True when the run is done and this application has its result, so it is in the list of runs.</summary>
    public bool ResultIsStored =>
        CurrentRun is { State: BenchmarkRunStates.Done, Delivery: BenchmarkDeliveryStates.Delivered };

    /// <summary>True when the choice is complete and no run is being followed.</summary>
    public bool CanStart =>
        !IsBusy
        && !IsFollowing
        && _selected.Count is >= 1 and <= MaxModels
        && (HotelAnalysis || DeterministicTasks)
        && Runs is >= 1 and <= MaxRuns;

    /// <summary>
    /// Loads the installed models when the page opens, then the recorded runs: a run that is still going is followed again,
    /// and a run to start again (<paramref name="againRunId"/>) has its choice taken.
    /// </summary>
    /// <param name="againRunId">A recorded run whose choice is to be taken, or <c>null</c>.</param>
    /// <param name="cancellationToken">Cancels the call when the page is closed.</param>
    /// <returns>A task that ends when the models and the runs are loaded.</returns>
    public async Task OpenAsync(Guid? againRunId, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        if (Models.Count == 0)
        {
            return;
        }

        var recent = await _tracker.LoadRecentAsync(cancellationToken).ConfigureAwait(false);
        if (recent.IsFailure)
        {
            // The runs could not be read; starting a new run still works.
            return;
        }

        if (againRunId is { } id && recent.Value.FirstOrDefault(record => record.RunId == id) is { } again)
        {
            UseChoiceOf(again);
            return;
        }

        if (recent.Value.FirstOrDefault(record => record.IsOpen) is { } open)
        {
            var state = await _tracker.RefreshAsync(open.RunId, open.State, cancellationToken).ConfigureAwait(false);
            if (state.IsSuccess)
            {
                UseChoiceOf(open);
                CurrentRun = state.Value;
                return;
            }
        }

        StoppedRun = recent.Value.FirstOrDefault(record => !record.IsOpen);
    }

    /// <summary>Takes the models and options of a recorded run, as far as its models are still installed.</summary>
    /// <param name="record">The recorded run.</param>
    public void UseChoiceOf(BenchmarkRequestDto record)
    {
        ArgumentNullException.ThrowIfNull(record);

        _selected.Clear();
        _selected.AddRange(record.Models.Where(Models.Contains).Take(MaxModels));
        HotelAnalysis = record.HotelAnalysis;
        DeterministicTasks = record.DeterministicTasks;
        Runs = Math.Clamp(record.Runs, 1, MaxRuns);
        StoppedRun = null;
    }

    /// <summary>Loads the installed models, also after a model was added or deleted.</summary>
    /// <param name="cancellationToken">Cancels the call when the page is closed.</param>
    /// <returns>A task that ends when the models are loaded.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        try
        {
            var result = await _client
                .RunAsync((client, token) => client.ListModelsAsync(token), cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                Models = [];
                return;
            }

            Errors = [];
            Models = result.Value;
        }
        finally
        {
            IsBusy = false;
            IsLoaded = true;
        }

        await RefreshLiveAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the runs and the installed models again, so the page follows a change that any caller made: a run that moved
    /// from queued to running, a run that someone else started, a model that was added or deleted. A failing read keeps the
    /// last state and sets <see cref="IsLive"/> to false.
    /// </summary>
    /// <param name="cancellationToken">Cancels the call when the page is closed.</param>
    /// <returns>A task that ends when the service has answered.</returns>
    public async Task RefreshLiveAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        var runs = await _client
            .RunAsync((client, token) => client.ListRunsAsync(token), cancellationToken)
            .ConfigureAwait(false);
        var models = await _client
            .RunAsync((client, token) => client.ListModelsAsync(token), cancellationToken)
            .ConfigureAwait(false);

        IsLive = runs.IsSuccess && models.IsSuccess;
        if (runs.IsSuccess)
        {
            KnownRuns = runs.Value;
            if (CurrentRun is { } current && runs.Value.FirstOrDefault(run => run.RunId == current.RunId) is { } fresh)
            {
                CurrentRun = fresh;
            }
        }

        if (models.IsSuccess && !models.Value.SequenceEqual(Models, StringComparer.Ordinal))
        {
            Models = models.Value;
            _selected.RemoveAll(model => !Models.Contains(model));
        }
    }

    /// <summary>Chooses a model, or takes it off again. A model that is not offered, or a fifth model, is ignored.</summary>
    /// <param name="model">The model name.</param>
    public void ToggleModel(string model)
    {
        if (!_selected.Remove(model) && Models.Contains(model) && _selected.Count < MaxModels)
        {
            _selected.Add(model);
        }
    }

    /// <summary>Starts a run with the chosen models and options.</summary>
    /// <param name="cancellationToken">Cancels the call when the page is closed.</param>
    /// <returns>A task that ends when the service has answered.</returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!CanStart)
        {
            return;
        }

        var request = new StartBenchmarkRunRequest([.. _selected], HotelAnalysis, DeterministicTasks, Runs);

        IsBusy = true;
        try
        {
            var result = await _client
                .RunAsync((client, token) => client.StartRunAsync(request, token), cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                return;
            }

            CurrentRun = result.Value;
            StoppedRun = null;

            // The run goes on even when it cannot be recorded; the page says so.
            var recorded = await _tracker.RecordStartedAsync(result.Value, request, cancellationToken).ConfigureAwait(false);
            Errors = recorded.IsSuccess ? [] : ErrorMessages.Describe(recorded);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Asks for the state of the run that is followed and records it. A run the service has forgotten is recorded as
    /// interrupted and ends the following; the choice stays, so the same run can be started again.
    /// </summary>
    /// <param name="cancellationToken">Cancels the call when the page is closed.</param>
    /// <returns>A task that ends when the service has answered.</returns>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentRun is not { } run)
        {
            return;
        }

        var result = await _tracker.RefreshAsync(run.RunId, run.State, cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            Errors = ErrorMessages.Describe(result);
            if (result.Errors.Any(error => error.Code == ErrorCodes.BenchmarkRunner.RunNotFound))
            {
                // The service restarted and forgot the run: there is nothing more to follow.
                CurrentRun = null;
            }

            return;
        }

        Errors = [];
        CurrentRun = result.Value;
    }
}
