using Microsoft.EntityFrameworkCore;

using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Infrastructure.Repositories;

/// <summary>
/// Stores the benchmark runs and reads them for the back office (ADR-0013). The reads never load the answer text
/// of a request except in <see cref="GetOutputAsync"/>.
/// </summary>
public sealed class BenchmarkRunRepository : Repository<BenchmarkRun, int>, IBenchmarkRunRepository, IBenchmarkQueries
{
    /// <summary>Creates the repository.</summary>
    /// <param name="context">The database context.</param>
    public BenchmarkRunRepository(HotelDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<BenchmarkRun?> FindByRunIdAsync(Guid runId, CancellationToken cancellationToken = default) =>
        await Context.BenchmarkRuns
            .FirstOrDefaultAsync(run => run.RunId == runId, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public void Remove(BenchmarkRun run) => MarkForRemoval(run);

    /// <inheritdoc />
    public async Task<IReadOnlyList<BenchmarkRunListItemDto>> GetRunsAsync(CancellationToken cancellationToken = default)
    {
        var runs = await Context.BenchmarkRuns
            .AsNoTracking()
            .OrderByDescending(run => run.StartedAt)
            .ThenByDescending(run => run.Id)
            .Select(run => new HeaderRow(
                run.Id, run.RunId, run.StartedAt, run.FinishedAt, run.DatasetName, run.Cpu, run.MemoryMb, run.Gpu, run.BackendName, run.BackendVersion))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var models = await Context.BenchmarkResults
            .AsNoTracking()
            .Select(result => new { result.BenchmarkRunId, result.Model })
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var counts = await Context.BenchmarkResults
            .AsNoTracking()
            .GroupBy(result => result.BenchmarkRunId)
            .Select(group => new { RunKey = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return runs
            .Select(run => run.ToDto(
                models
                    .Where(pair => pair.BenchmarkRunId == run.Id)
                    .Select(pair => pair.Model)
                    .Order(StringComparer.Ordinal)
                    .ToArray(),
                counts.FirstOrDefault(count => count.RunKey == run.Id)?.Count ?? 0))
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<BenchmarkRunDetailsDto?> GetRunAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await Context.BenchmarkRuns
            .AsNoTracking()
            .Where(candidate => candidate.RunId == runId)
            .Select(candidate => new
            {
                Header = new HeaderRow(
                    candidate.Id,
                    candidate.RunId,
                    candidate.StartedAt,
                    candidate.FinishedAt,
                    candidate.DatasetName,
                    candidate.Cpu,
                    candidate.MemoryMb,
                    candidate.Gpu,
                    candidate.BackendName,
                    candidate.BackendVersion),
                candidate.DatasetHash,
                candidate.DatasetRows,
                candidate.PromptVersion,
                candidate.CpuCores,
                candidate.OperatingSystem,
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (run is null)
        {
            return null;
        }

        var results = await Context.BenchmarkResults
            .AsNoTracking()
            .Where(result => result.BenchmarkRunId == run.Header.Id)
            .OrderBy(result => result.Id)
            .Select(result => new BenchmarkResultDto(
                result.Id,
                result.Timestamp,
                result.Model,
                result.AnalysisType,
                result.TaskId,
                result.RunNumber,
                result.Cold,
                result.TimeToFirstTokenSeconds,
                result.TotalSeconds,
                result.CompletionTokens,
                result.TokensPerSecond,
                result.TokensEstimated,
                result.RamPeakMb,
                result.VramPeakMb,
                result.Passed,
                result.ResponseValid,
                result.Error != null,
                result.RamBeforeMb,
                result.RamAfterMb,
                result.VramBeforeMb,
                result.VramAfterMb,
                result.FinishReason))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var models = results.Select(result => result.Model).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var header = run.Header.ToDto(models, results.Count);

        return new BenchmarkRunDetailsDto(
            header,
            run.DatasetHash,
            run.DatasetRows,
            run.PromptVersion,
            run.CpuCores,
            run.OperatingSystem,
            results);
    }

    /// <inheritdoc />
    public async Task<BenchmarkOutputDto?> GetOutputAsync(int benchmarkResultId, CancellationToken cancellationToken = default) =>
        await Context.BenchmarkResults
            .AsNoTracking()
            .Where(result => result.Id == benchmarkResultId)
            .Select(result => new BenchmarkOutputDto(
                result.Id,
                result.Model,
                result.Output,
                result.ValidationError,
                result.Error))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    private sealed record HeaderRow(
        int Id,
        Guid RunId,
        DateTimeOffset StartedAt,
        DateTimeOffset FinishedAt,
        string DatasetName,
        string Cpu,
        int MemoryMb,
        string? Gpu,
        string BackendName,
        string? BackendVersion)
    {
        public BenchmarkRunListItemDto ToDto(IReadOnlyList<string> models, int requestCount) =>
            new(RunId, StartedAt, FinishedAt, DatasetName, Cpu, MemoryMb, Gpu, BackendName, BackendVersion, models, requestCount);
    }
}
