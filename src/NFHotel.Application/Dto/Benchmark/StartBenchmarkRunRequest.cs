namespace NFHotel.Application.Dto.Benchmark;

/// <summary>What a Manager asks the benchmark service for: the models and the options of a run (ADR-0001 of the benchmark service).</summary>
/// <param name="Models">One to four installed model names.</param>
/// <param name="HotelAnalysis">True to make the hotel cancellation analysis.</param>
/// <param name="DeterministicTasks">True to make the deterministic tasks.</param>
/// <param name="Runs">Requests per model and task, from 1 to 5.</param>
public sealed record StartBenchmarkRunRequest(
    IReadOnlyList<string> Models,
    bool HotelAnalysis = true,
    bool DeterministicTasks = false,
    int Runs = 1);