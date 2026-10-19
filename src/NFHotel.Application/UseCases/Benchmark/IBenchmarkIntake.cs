using NFHotel.Application.Dto.Benchmark;

namespace NFHotel.Application.UseCases.Benchmark;

/// <summary>
/// Receives the runs that the benchmark service delivers (ADR-0008). Only the API implements it: the web application
/// never stores a run itself.
/// </summary>
public interface IBenchmarkIntake
{
    /// <summary>
    /// Stores a delivered run once. A run id that is already stored answers <see cref="BenchmarkStoreOutcome.AlreadyStored"/>
    /// whatever the body says, so a repeat after a failed delivery is harmless.
    /// </summary>
    /// <param name="request">The body of the contract.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Created or AlreadyStored; or the codes of what is wrong with the body.</returns>
    Task<Result<BenchmarkStoreOutcome>> StoreAsync(
        SubmitBenchmarkRunRequest request,
        CancellationToken cancellationToken = default);
}