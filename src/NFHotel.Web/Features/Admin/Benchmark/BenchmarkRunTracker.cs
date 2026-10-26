using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Benchmark;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Benchmark;

/// <summary>
/// Keeps the hotel's record of the benchmark runs in step with the benchmark service (ADR-0031). The service forgets its
/// runs when it restarts, so every state the back office reads is recorded through the API. A run the service no longer
/// knows is recorded as done when its result is stored, and as interrupted otherwise (MIL-046). Shared by the start page
/// and the list of runs.
/// </summary>
public sealed class BenchmarkRunTracker
{
    /// <summary>The error recorded for a run that the service forgot.</summary>
    public const string InterruptedReason = "The benchmark service was restarted before the run ended.";

    private readonly IUseCaseRunner<IBenchmarkServiceClient> _client;
    private readonly IUseCaseRunner<IBenchmarkRequestService> _requests;
    private readonly IUseCaseRunner<IBenchmarkService> _stored;

    /// <summary>Creates the tracker.</summary>
    /// <param name="client">Runs the calls to the benchmark service.</param>
    /// <param name="requests">Runs the calls that record the runs through the API.</param>
    /// <param name="stored">Reads the stored results through the API, which prove that a forgotten run finished.</param>
    public BenchmarkRunTracker(
        IUseCaseRunner<IBenchmarkServiceClient> client,
        IUseCaseRunner<IBenchmarkRequestService> requests,
        IUseCaseRunner<IBenchmarkService> stored)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(stored);

        _client = client;
        _requests = requests;
        _stored = stored;
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
    /// recorded as done, with its stored result as the answer, when that result is stored; otherwise it is recorded as
    /// interrupted and the answer is the failure with the run-not-found code.
    /// </summary>
    /// <param name="runId">The run.</param>
    /// <param name="knownState">The state recorded or shown last, or <c>null</c>.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The state as the service reports it, the state of the stored result, or the failure.</returns>
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
            var stored = await FindStoredAsync(runId, cancellationToken).ConfigureAwait(false);
            if (stored is not null)
            {
                await RecordStateAsync(runId, BenchmarkRunStates.Done, null, cancellationToken).ConfigureAwait(false);
                return Result<BenchmarkRunStateDto>.Success(DoneState(stored));
            }

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
    /// one and the stored results about every interrupted one, so a run that a restart stopped, or that finished, while
    /// nobody looked is shown truly.
    /// </summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The records that are not done, newest first, or why they could not be read.</returns>
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
            var current = record.IsOpen || record.State == BenchmarkRunStates.Interrupted
                ? await BringUpToDateAsync(record, cancellationToken).ConfigureAwait(false)
                : record;

            // A run that turned out done has its stored result; the list holds the runs without one.
            if (current.State != BenchmarkRunStates.Done)
            {
                records.Add(current);
            }
        }

        return Result<IReadOnlyList<BenchmarkRequestDto>>.Success(records);
    }

    private async Task<BenchmarkRequestDto> BringUpToDateAsync(BenchmarkRequestDto record, CancellationToken cancellationToken)
    {
        if (record.State == BenchmarkRunStates.Interrupted)
        {
            // The service forgot it long ago; only a stored result can change what is known about it.
            if (await FindStoredAsync(record.RunId, cancellationToken).ConfigureAwait(false) is null)
            {
                return record;
            }

            await RecordStateAsync(record.RunId, BenchmarkRunStates.Done, null, cancellationToken).ConfigureAwait(false);
            return record with { State = BenchmarkRunStates.Done, Error = null };
        }

        var state = await RefreshAsync(record.RunId, record.State, cancellationToken).ConfigureAwait(false);
        if (IsForgotten(state))
        {
            return record with { State = BenchmarkRunStates.Interrupted, Error = InterruptedReason };
        }

        // A service that cannot be reached leaves the record as it was: the next look will try again.
        return state.IsSuccess ? record with { State = state.Value.State, Error = state.Value.Error } : record;
    }

    private async Task<BenchmarkRunDetailsDto?> FindStoredAsync(Guid runId, CancellationToken cancellationToken)
    {
        // Not found, or an API that cannot be reached, is "no proof": the run is then not marked done.
        var stored = await _stored
            .RunAsync((service, token) => service.GetRunAsync(runId, token), cancellationToken)
            .ConfigureAwait(false);

        return stored.IsSuccess ? stored.Value : null;
    }

    private async Task RecordStateAsync(Guid runId, string state, string? error, CancellationToken cancellationToken) =>
        // A record that cannot be written (the API is down) is tried again at the next look; the page still shows the state.
        _ = await _requests
            .RunAsync(
                (service, token) => service.SetStateAsync(runId, new SetBenchmarkRequestStateRequest(state, error), token),
                cancellationToken)
            .ConfigureAwait(false);

    private static BenchmarkRunStateDto DoneState(BenchmarkRunDetailsDto stored) =>
        new(
            stored.Header.RunId,
            BenchmarkRunStates.Done,
            BenchmarkDeliveryStates.Delivered,
            stored.Header.Models,
            stored.Header.StartedAt,
            stored.Header.StartedAt,
            stored.Header.FinishedAt,
            null);

    private static bool IsForgotten(Result result) =>
        result.Errors.Any(error => error.Code == ErrorCodes.BenchmarkRunner.RunNotFound);
}
