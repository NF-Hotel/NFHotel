using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Benchmark;

/// <summary>Stores and reads the benchmark runs (ADR-0013).</summary>
public sealed class BenchmarkService : IBenchmarkService, IBenchmarkIntake
{
    /// <summary>The only result schema version that is read.</summary>
    public const int SupportedSchemaVersion = 2;

    private readonly IBenchmarkRunRepository _runs;
    private readonly IBenchmarkQueries _queries;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Creates the service.</summary>
    /// <param name="runs">The storage of runs.</param>
    /// <param name="queries">The reads.</param>
    /// <param name="unitOfWork">Saves the changes.</param>
    public BenchmarkService(IBenchmarkRunRepository runs, IBenchmarkQueries queries, IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(runs);
        ArgumentNullException.ThrowIfNull(queries);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _runs = runs;
        _queries = queries;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<BenchmarkStoreOutcome>> StoreAsync(
        SubmitBenchmarkRunRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // A repeat is answered before the body is looked at: a delivery that was retried must not fail for a defect
        // that the first delivery did not have, and it must not store the run again.
        if (request.RunId != Guid.Empty
            && await _runs.FindByRunIdAsync(request.RunId, cancellationToken).ConfigureAwait(false) is not null)
        {
            return Result<BenchmarkStoreOutcome>.Success(BenchmarkStoreOutcome.AlreadyStored);
        }

        if (request.SchemaVersion != SupportedSchemaVersion)
        {
            return Result<BenchmarkStoreOutcome>.Failure(ErrorCodes.Benchmark.SchemaVersionUnsupported);
        }

        if (request.Dataset is null
            || request.Machine is null
            || request.Backend is null
            || request.Results is null
            || request.Results.Any(result => result is null))
        {
            return Result<BenchmarkStoreOutcome>.Failure(ErrorCodes.Benchmark.BodyInvalid);
        }

        BenchmarkRun run;
        try
        {
            run = BenchmarkRun.Create(
                request.RunId,
                request.StartedAt,
                request.FinishedAt,
                request.Dataset.Name,
                request.Dataset.Sha256,
                request.Dataset.Rows,
                request.PromptVersion,
                new BenchmarkMachine(
                    request.Machine.Cpu,
                    request.Machine.CpuCores,
                    request.Machine.MemoryMb,
                    request.Machine.Gpu,
                    request.Machine.Os),
                request.Backend.Name,
                request.Backend.Version,
                request.Results.Select(ToData).ToArray());
        }
        catch (DomainException exception)
        {
            return Result<BenchmarkStoreOutcome>.FromCodes(exception.Codes);
        }

        _runs.Add(run);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (UniqueConstraintViolationException)
        {
            // Two deliveries at the same moment: the other one stored the run first.
            return Result<BenchmarkStoreOutcome>.Success(BenchmarkStoreOutcome.AlreadyStored);
        }

        return Result<BenchmarkStoreOutcome>.Success(BenchmarkStoreOutcome.Created);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<BenchmarkRunListItemDto>>> GetRunsAsync(
        CancellationToken cancellationToken = default)
    {
        var runs = await _queries.GetRunsAsync(cancellationToken).ConfigureAwait(false);

        return Result<IReadOnlyList<BenchmarkRunListItemDto>>.Success(runs);
    }

    /// <inheritdoc />
    public async Task<Result<BenchmarkRunDetailsDto>> GetRunAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        if (runId == Guid.Empty)
        {
            return Result<BenchmarkRunDetailsDto>.Failure(ErrorCodes.Benchmark.NotFound);
        }

        var details = await _queries.GetRunAsync(runId, cancellationToken).ConfigureAwait(false);

        return details is null
            ? Result<BenchmarkRunDetailsDto>.Failure(ErrorCodes.Benchmark.NotFound)
            : Result<BenchmarkRunDetailsDto>.Success(details);
    }

    /// <inheritdoc />
    public async Task<Result<BenchmarkOutputDto>> GetOutputAsync(
        int benchmarkResultId,
        CancellationToken cancellationToken = default)
    {
        var output = await _queries.GetOutputAsync(benchmarkResultId, cancellationToken).ConfigureAwait(false);

        return output is null
            ? Result<BenchmarkOutputDto>.Failure(ErrorCodes.Benchmark.NotFound)
            : Result<BenchmarkOutputDto>.Success(output);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteRunAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        if (runId == Guid.Empty)
        {
            return Result.Failure(ErrorCodes.Benchmark.NotFound);
        }

        var run = await _runs.FindByRunIdAsync(runId, cancellationToken).ConfigureAwait(false);
        if (run is null)
        {
            return Result.Failure(ErrorCodes.Benchmark.NotFound);
        }

        _runs.Remove(run);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    private static BenchmarkResultData ToData(BenchmarkRequestResultDto dto) =>
        new(
            dto.Timestamp,
            dto.Backend,
            dto.Model,
            dto.AnalysisType,
            dto.TaskId,
            dto.Category,
            dto.Run,
            dto.Cold,
            dto.TtftSeconds,
            dto.TotalSeconds,
            dto.CompletionTokens,
            dto.PromptTokens,
            dto.TokensPerSecond,
            dto.TokensEstimated,
            dto.FinishReason,
            dto.SystemRamBeforeMb,
            dto.SystemRamAfterMb,
            dto.SystemRamPeakMb,
            dto.GpuVramBeforeMb,
            dto.GpuVramAfterMb,
            dto.GpuVramPeakMb,
            dto.Passed,
            dto.ResponseValid,
            dto.ValidationError,
            dto.Output,
            dto.Error);
}
