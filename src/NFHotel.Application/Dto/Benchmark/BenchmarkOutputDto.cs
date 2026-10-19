namespace NFHotel.Application.Dto.Benchmark;

/// <summary>The answer text and the error of one request, read on its own because they can be long.</summary>
/// <param name="BenchmarkResultId">The key of the request.</param>
/// <param name="Model">The model.</param>
/// <param name="Output">The answer text, or <c>null</c>.</param>
/// <param name="ValidationError">Why the answer was invalid, or <c>null</c>.</param>
/// <param name="Error">The error of the request, or <c>null</c>.</param>
public sealed record BenchmarkOutputDto(
    int BenchmarkResultId,
    string Model,
    string? Output,
    string? ValidationError,
    string? Error);