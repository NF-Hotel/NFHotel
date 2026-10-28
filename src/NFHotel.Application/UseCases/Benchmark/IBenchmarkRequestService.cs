using NFHotel.Application.Dto.Benchmark;

namespace NFHotel.Application.UseCases.Benchmark;

/// <summary>
/// Records the benchmark runs a Manager starts and the states the benchmark service reports, so a run that a restart of
/// the service loses is still shown (ADR-0031). The API implements it with the repository, and the web application with
/// a client over HTTP.
/// </summary>
public interface IBenchmarkRequestService
{
    /// <summary>The days a run that is not done stays in <see cref="GetRecentAsync"/>.</summary>
    const int RecentDays = 7;

    /// <summary>Records a run the service has just accepted. Recording the same run id again changes nothing.</summary>
    /// <param name="request">The run id and the choice.</param>
    /// <param name="requestedBy">The user name of the staff member; the API reads it from the staff token.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The record, or the codes that say what is wrong.</returns>
    Task<Result<BenchmarkRequestDto>> RecordAsync(
        RecordBenchmarkRequest request,
        string requestedBy,
        CancellationToken cancellationToken = default);

    /// <summary>Records the state the service reported for a run.</summary>
    /// <param name="runId">The run id.</param>
    /// <param name="request">The state and the error.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The record after the change; the not found code, or the code of a refused change.</returns>
    Task<Result<BenchmarkRequestDto>> SetStateAsync(
        Guid runId,
        SetBenchmarkRequestStateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Lists the runs of the last <see cref="RecentDays"/> days that are not done, newest first.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The records.</returns>
    Task<Result<IReadOnlyList<BenchmarkRequestDto>>> GetRecentAsync(CancellationToken cancellationToken = default);
}
