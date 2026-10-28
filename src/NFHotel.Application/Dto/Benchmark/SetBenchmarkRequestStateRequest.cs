namespace NFHotel.Application.Dto.Benchmark;

/// <summary>The body that records the state the benchmark service reported for a run (ADR-0031).</summary>
/// <param name="State">queued, running, done, failed or interrupted.</param>
/// <param name="Error">Why it failed or was interrupted, or <c>null</c>.</param>
public sealed record SetBenchmarkRequestStateRequest(string State, string? Error = null);
