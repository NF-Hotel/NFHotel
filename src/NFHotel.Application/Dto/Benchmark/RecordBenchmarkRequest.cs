namespace NFHotel.Application.Dto.Benchmark;

/// <summary>The body that records a run the benchmark service has just accepted (ADR-0031).</summary>
/// <param name="RunId">The id the service gave the run.</param>
/// <param name="Models">The chosen models.</param>
/// <param name="HotelAnalysis">True for the hotel cancellation analysis.</param>
/// <param name="DeterministicTasks">True for the deterministic tasks.</param>
/// <param name="Runs">Requests per model and task.</param>
public sealed record RecordBenchmarkRequest(
    Guid RunId,
    IReadOnlyList<string> Models,
    bool HotelAnalysis,
    bool DeterministicTasks,
    int Runs);
