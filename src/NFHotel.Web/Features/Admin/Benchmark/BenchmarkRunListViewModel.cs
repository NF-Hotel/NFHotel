using NFHotel.Application.Dto.AiDefaults;
using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.UseCases.AiDefaults;
using NFHotel.Application.UseCases.Benchmark;
using NFHotel.Domain.Entities;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Benchmark;

/// <summary>
/// State of the page of stored benchmark runs (ADR-0013): the list, the run that is open and the answer of one request.
/// The page holds only markup and binding.
/// </summary>
public sealed class BenchmarkRunListViewModel
{
    private readonly IUseCaseRunner<IBenchmarkService> _benchmark;
    private readonly IUseCaseRunner<IAiDefaultService> _aiDefaults;
    private readonly Dictionary<string, string> _drafts = [];

    /// <summary>Creates the view model.</summary>
    /// <param name="benchmark">Runs the use cases with the staff token of the signed-in user.</param>
    /// <param name="aiDefaults">Reads and sets the default AI models with the same token.</param>
    public BenchmarkRunListViewModel(IUseCaseRunner<IBenchmarkService> benchmark, IUseCaseRunner<IAiDefaultService> aiDefaults)
    {
        ArgumentNullException.ThrowIfNull(benchmark);
        ArgumentNullException.ThrowIfNull(aiDefaults);

        _benchmark = benchmark;
        _aiDefaults = aiDefaults;
    }

    /// <summary>The default AI models that are set, by purpose.</summary>
    public IReadOnlyList<AiDefaultDto> Defaults { get; private set; } = [];

    /// <summary>The models seen in the stored runs, in alphabetical order: what the Manager can choose from.</summary>
    public IReadOnlyList<string> KnownModels =>
        [.. Runs.SelectMany(run => run.Models).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

    /// <summary>The model that is set as the default of a purpose, or <c>null</c> when none is set.</summary>
    /// <param name="purpose">The purpose.</param>
    /// <returns>The default, or <c>null</c>.</returns>
    public AiDefaultDto? DefaultOf(string purpose) => Defaults.FirstOrDefault(value => value.Purpose == purpose);

    /// <summary>The model typed or chosen in the field of a purpose; the saved default until the user changes it.</summary>
    /// <param name="purpose">The purpose.</param>
    /// <returns>The text of the field.</returns>
    public string DraftOf(string purpose) =>
        _drafts.TryGetValue(purpose, out var draft) ? draft : DefaultOf(purpose)?.Model ?? string.Empty;

    /// <summary>Remembers the text of the field of a purpose.</summary>
    /// <param name="purpose">The purpose.</param>
    /// <param name="model">The text.</param>
    public void SetDraft(string purpose, string? model) => _drafts[purpose] = model ?? string.Empty;

    /// <summary>Sets the default of a purpose to the model in its field.</summary>
    /// <param name="purpose">The purpose.</param>
    /// <param name="cancellationToken">Cancels the call when the page is closed.</param>
    /// <returns>A task that ends when the default is saved and read again.</returns>
    public Task SaveDefaultAsync(string purpose, CancellationToken cancellationToken = default)
    {
        var model = DraftOf(purpose).Trim();
        var source = Runs.FirstOrDefault(run => run.Models.Contains(model, StringComparer.Ordinal))?.RunId;

        return SetDefaultAsync(purpose, model, source, cancellationToken);
    }

    /// <summary>Sets the default of a purpose to the model of a result row, remembering the run.</summary>
    /// <param name="purpose">The purpose.</param>
    /// <param name="model">The model of the row.</param>
    /// <param name="runId">The run of the row.</param>
    /// <param name="cancellationToken">Cancels the call when the page is closed.</param>
    /// <returns>A task that ends when the default is saved and read again.</returns>
    public async Task SetDefaultAsync(string purpose, string model, Guid? runId, CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        try
        {
            var result = await _aiDefaults
                .RunAsync((service, token) => service.SetAsync(purpose, new SetAiDefaultRequest(model, runId), string.Empty, token), cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                return;
            }

            Errors = [];
            _drafts.Remove(purpose);
        }
        finally
        {
            IsBusy = false;
        }

        await LoadDefaultsAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>The words that name a purpose in a label, for example "the website".</summary>
    /// <param name="purpose">The purpose.</param>
    /// <returns>The label to show.</returns>
    public string LabelOfPurpose(string purpose) =>
        purpose == AiDefault.Website ? "the website" : "the market analysis";

    private async Task LoadDefaultsAsync(CancellationToken cancellationToken)
    {
        var result = await _aiDefaults
            .RunAsync((service, token) => service.GetAllAsync(token), cancellationToken)
            .ConfigureAwait(false);

        // A failing read of the defaults does not hide the runs; the card then shows no default.
        Defaults = result.IsFailure ? [] : result.Value;
    }

    /// <summary>The stored runs, newest first.</summary>
    public IReadOnlyList<BenchmarkRunListItemDto> Runs { get; private set; } = [];

    /// <summary>The run that is open, or <c>null</c>.</summary>
    public BenchmarkRunDetailsDto? SelectedRun { get; private set; }

    /// <summary>The run that waits for the confirmation of its deletion, or <c>null</c>.</summary>
    public BenchmarkRunListItemDto? RunPendingDeletion { get; private set; }

    /// <summary>The answer of one request that is shown, or <c>null</c>.</summary>
    public BenchmarkOutputDto? ShownOutput { get; private set; }

    /// <summary>The texts of the last refusal, empty when the last call succeeded.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = [];

    /// <summary>True while a call is running.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>True once the list has been asked for, with or without an answer.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>True when the list was read and holds no run.</summary>
    public bool IsEmpty => IsLoaded && Errors.Count == 0 && Runs.Count == 0;

    /// <summary>Loads the list when the page opens.</summary>
    /// <param name="cancellationToken">Cancels the call when the page is closed.</param>
    /// <returns>A task that ends when the list is loaded.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        try
        {
            var result = await _benchmark
                .RunAsync((service, token) => service.GetRunsAsync(token), cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                Runs = [];
                return;
            }

            Errors = [];
            Runs = result.Value;
            await LoadDefaultsAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
            IsLoaded = true;
        }
    }

    /// <summary>Opens a run, or closes it when it is the one that is open.</summary>
    /// <param name="runId">The run id.</param>
    /// <param name="cancellationToken">Cancels the call when the page is closed.</param>
    /// <returns>A task that ends when the run is loaded.</returns>
    public async Task SelectAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        ShownOutput = null;
        if (SelectedRun?.Header.RunId == runId)
        {
            SelectedRun = null;
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _benchmark
                .RunAsync((service, token) => service.GetRunAsync(runId, token), cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                SelectedRun = null;
                return;
            }

            Errors = [];
            SelectedRun = result.Value;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Reads the answer of one request on its own.</summary>
    /// <param name="benchmarkResultId">The key of the request.</param>
    /// <param name="cancellationToken">Cancels the call when the page is closed.</param>
    /// <returns>A task that ends when the answer is read.</returns>
    public async Task ShowOutputAsync(int benchmarkResultId, CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        try
        {
            var result = await _benchmark
                .RunAsync((service, token) => service.GetOutputAsync(benchmarkResultId, token), cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                ShownOutput = null;
                return;
            }

            Errors = [];
            ShownOutput = result.Value;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Asks for the confirmation of deleting a run.</summary>
    /// <param name="run">The run to delete.</param>
    public void RequestDelete(BenchmarkRunListItemDto run)
    {
        ArgumentNullException.ThrowIfNull(run);

        RunPendingDeletion = run;
    }

    /// <summary>Backs out of deleting a run.</summary>
    public void AbortDelete() => RunPendingDeletion = null;

    /// <summary>Deletes the run that the user confirmed, for good, and reads the list again.</summary>
    /// <param name="cancellationToken">Cancels the call when the page is closed.</param>
    /// <returns>A task that ends when the list is read again.</returns>
    public async Task ConfirmDeleteAsync(CancellationToken cancellationToken = default)
    {
        var run = RunPendingDeletion;
        if (run is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _benchmark
                .RunAsync((service, token) => service.DeleteRunAsync(run.RunId, token), cancellationToken)
                .ConfigureAwait(false);

            RunPendingDeletion = null;
            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                return;
            }

            if (SelectedRun?.Header.RunId == run.RunId)
            {
                SelectedRun = null;
            }

            ShownOutput = null;
        }
        finally
        {
            IsBusy = false;
        }

        await InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Hides the answer that is shown.</summary>
    public void HideOutput() => ShownOutput = null;
}
