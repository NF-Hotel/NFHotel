using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.UseCases.Benchmark;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Benchmark;

/// <summary>
/// State of the page of stored benchmark runs (ADR-0013): the list, the run that is open and the answer of one request.
/// The page holds only markup and binding.
/// </summary>
public sealed class BenchmarkRunListViewModel
{
    private readonly IUseCaseRunner<IBenchmarkService> _benchmark;

    /// <summary>Creates the view model.</summary>
    /// <param name="benchmark">Runs the use cases with the staff token of the signed-in user.</param>
    public BenchmarkRunListViewModel(IUseCaseRunner<IBenchmarkService> benchmark)
    {
        ArgumentNullException.ThrowIfNull(benchmark);

        _benchmark = benchmark;
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
