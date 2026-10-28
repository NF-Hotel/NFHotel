using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Repositories;

namespace NFHotel.Application.UseCases.Benchmark;

/// <summary>Records the benchmark runs a Manager starts and their states (ADR-0031).</summary>
public sealed class BenchmarkRequestService : IBenchmarkRequestService
{
    private readonly IBenchmarkRequestRepository _requests;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    /// <summary>Creates the service.</summary>
    /// <param name="requests">The storage of the records.</param>
    /// <param name="unitOfWork">Saves the changes.</param>
    /// <param name="clock">The clock that says when a run was recorded or changed.</param>
    public BenchmarkRequestService(IBenchmarkRequestRepository requests, IUnitOfWork unitOfWork, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);

        _requests = requests;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result<BenchmarkRequestDto>> RecordAsync(
        RecordBenchmarkRequest request,
        string requestedBy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = await _requests.FindByRunIdAsync(request.RunId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return Result<BenchmarkRequestDto>.Success(ToDto(existing));
        }

        BenchmarkRequest recorded;
        try
        {
            recorded = BenchmarkRequest.Create(
                request.RunId,
                request.Models ?? [],
                request.HotelAnalysis,
                request.DeterministicTasks,
                request.Runs,
                requestedBy,
                _clock.UtcNow);
        }
        catch (DomainException exception)
        {
            return Result<BenchmarkRequestDto>.FromCodes(exception.Codes);
        }

        _requests.Add(recorded);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<BenchmarkRequestDto>.Success(ToDto(recorded));
    }

    /// <inheritdoc />
    public async Task<Result<BenchmarkRequestDto>> SetStateAsync(
        Guid runId,
        SetBenchmarkRequestStateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stored = await _requests.FindByRunIdAsync(runId, cancellationToken).ConfigureAwait(false);
        if (stored is null)
        {
            return Result<BenchmarkRequestDto>.Failure(ErrorCodes.Benchmark.NotFound);
        }

        try
        {
            if (!stored.ChangeState(request.State, request.Error, _clock.UtcNow))
            {
                return Result<BenchmarkRequestDto>.Success(ToDto(stored));
            }
        }
        catch (DomainException exception)
        {
            return Result<BenchmarkRequestDto>.FromCodes(exception.Codes);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<BenchmarkRequestDto>.Success(ToDto(stored));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<BenchmarkRequestDto>>> GetRecentAsync(CancellationToken cancellationToken = default)
    {
        var since = _clock.UtcNow.AddDays(-IBenchmarkRequestService.RecentDays);
        var requests = await _requests.ListNotDoneSinceAsync(since, cancellationToken).ConfigureAwait(false);

        return Result<IReadOnlyList<BenchmarkRequestDto>>.Success(requests.Select(ToDto).ToArray());
    }

    private static BenchmarkRequestDto ToDto(BenchmarkRequest value) =>
        new(
            value.RunId,
            [.. value.Models],
            value.HotelAnalysis,
            value.DeterministicTasks,
            value.Runs,
            value.RequestedBy,
            value.RequestedAt,
            value.State,
            value.Error,
            value.UpdatedAt);
}
