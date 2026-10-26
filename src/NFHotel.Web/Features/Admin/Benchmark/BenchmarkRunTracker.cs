using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Benchmark;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Benchmark;

/// <summary>
/// Keeps the hotel's record of the benchmark runs in step with the benchmark service (ADR-0031). The service forgets its
/// runs when it restarts, so every state the back office reads is recorded through the API, and a run the service no
/// longer knows is recorded as interrupted. Shared by the start page and the list of runs.
/// </summary>
public sealed class BenchmarkRunTracker
{
    /// <summary>The error recorded for a run that the service forgot.</summary>
    public const string InterruptedReason = "The benchmark service was restarted before the run ended.";

    private readonly IUseCaseRunner<IBenchmarkServiceClient> _client;
    private readonly IUseCaseRunner<IBenchmarkRequestService> _requests;

    /// <summary>Creates the tracker.</summary>
    /// <param name="client">Runs the calls to the benchmark service.</param>
    /// <param name="requests">Runs the calls that record the runs through the API.</param>
    public BenchmarkRunTracker(
        IUseCaseRunner<IBenchmarkServiceClient> client,
        IUseCaseRunner<IBenchmarkRequestService> requests)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(requests);

        _client = client;
        _requests = requests;
    }

    /// <summary>Records a run that the service has just accepted, with its choice.</summary>
    /// <param name="run">The state the service answered.</param>
    /// <param name="choice">The models and options of the run.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Success, or why the record could not be made; the run goes on either way.</returns>
    public async Task<Result> RecordStartedAsync(
        BenchmarkRunStateDto run,
        StartBenchmarkRunRequest choice,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(choice);

        var request = new RecordBenchmarkRequest(run.RunId, choice.Models, choice.HotelAnalysis, choice.DeterministicTasks, choice.Runs);
        var result = await _requests
            .RunAsync((service, token) => service.RecordAsync(request, string.Empty, token), cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess ? Result.Success() : Result.Failure(result.Errors);
    }

    /// <summary>
    /// Asks the service for the state of a run and records it when it changed. A run the service no longer knows is
    /// recorded as interrupted; the answer is then the failure with the run-not-found code.
    /// </summary>
    /// <param name="runId">The run.</param>
    /// <param name="knownState">The state recorded or shown last, or <c>null</c>.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The state as the service reports it, or its failure.</returns>
    public async Task<Result<BenchmarkRunStateDto>> RefreshAsync(
        Guid runId,
        string? knownState,
        CancellationToken cancellationToken = default)
    {
        var result = await _client
            .RunAsync((client, token) => client.GetRunAsync(runId, token), cancellationToken)
            .ConfigureAwait(false);

        if (IsForgotten(result))
        {
            await RecordStateAsync(runId, BenchmarkRunStates.Interrupted, InterruptedReason, cancellationToken).ConfigureAwait(false);
        }
        else if (result.IsSuccess && result.Value.State != knownState)
        {
            await RecordStateAsync(runId, result.Value.State, result.Value.Error, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Lists the recorded runs of the last days that are not done, after asking the service about every queued or running
    /// one, so a run that a restart stopped while nobody looked is shown as interrupted.
    /// </summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The records, newest first, or why they could not be read.</returns>
    public async Task<Result<IReadOnlyList<BenchmarkRequestDto>>> LoadRecentAsync(CancellationToken cancellationToken = default)
    {
        var listed = await _requests
            .RunAsync((service, token) => service.GetRecentAsync(token), cancellationToken)
            .ConfigureAwait(false);
        if (listed.IsFailure)
        {
            return listed;
        }

        var records = new List<BenchmarkRequestDto>(listed.Value.Count);
        foreach (var record in listed.Value)
        {
            records.Add(record.IsOpen ? await BringUpToDateAsync(record, cancellationToken).ConfigureAwait(false) : record);
        }

        return Result<IReadOnlyList<BenchmarkRequestDto>>.Success(records);
    }

    private async Task<BenchmarkRequestDto> BringUpToDateAsync(BenchmarkRequestDto record, CancellationToken cancellationToken)
    {
        var state = await RefreshAsync(record.RunId, record.State, cancellationToken).ConfigureAwait(false);
        if (IsForgotten(state))
        {
            return record with { State = BenchmarkRunStates.Interrupted, Error = InterruptedReason };
        }

        // A service that cannot be reached leaves the record as it was: the next look will try again.
        return state.IsSuccess ? record with { State = state.Value.State, Error = state.Value.Error } : record;
    }

    private async Task RecordStateAsync(Guid runId, string state, string? error, CancellationToken cancellationToken) =>
        // A record that cannot be written (the API is down) is tried again at the next look; the page still shows the state.
        _ = await _requests
            .RunAsync(
                (service, token) => service.SetStateAsync(runId, new SetBenchmarkRequestStateRequest(state, error), token),
                cancellationToken)
            .ConfigureAwait(false);

    private static bool IsForgotten(Result result) =>
        result.Errors.Any(error => error.Code == ErrorCodes.BenchmarkRunner.RunNotFound);
}
