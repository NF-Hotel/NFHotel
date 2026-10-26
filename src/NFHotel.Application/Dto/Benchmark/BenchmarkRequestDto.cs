namespace NFHotel.Application.Dto.Benchmark;

/// <summary>The record of a started benchmark run (ADR-0031).</summary>
/// <param name="RunId">The id the benchmark service gave the run.</param>
/// <param name="Models">The chosen models, in the order they were chosen.</param>
/// <param name="HotelAnalysis">True when the hotel cancellation analysis was chosen.</param>
/// <param name="DeterministicTasks">True when the deterministic tasks were chosen.</param>
/// <param name="Runs">Requests per model and task.</param>
/// <param name="RequestedBy">The user name of the staff member who started it.</param>
/// <param name="RequestedAt">When it was started.</param>
/// <param name="State">The last state: queued, running, done, failed or interrupted.</param>
/// <param name="Error">Why it failed or was interrupted, or <c>null</c>.</param>
/// <param name="UpdatedAt">When the state was last changed.</param>
public sealed record BenchmarkRequestDto(
    Guid RunId,
    IReadOnlyList<string> Models,
    bool HotelAnalysis,
    bool DeterministicTasks,
    int Runs,
    string RequestedBy,
    DateTimeOffset RequestedAt,
    string State,
    string? Error,
    DateTimeOffset UpdatedAt)
{
    /// <summary>True when the service still has the run: queued or running.</summary>
    public bool IsOpen => State is BenchmarkRunStates.Queued or BenchmarkRunStates.Running;

    /// <summary>The choice of the run, to start the same one again.</summary>
    /// <returns>The request for the benchmark service.</returns>
    public StartBenchmarkRunRequest ToStartRequest() => new(Models, HotelAnalysis, DeterministicTasks, Runs);
}
