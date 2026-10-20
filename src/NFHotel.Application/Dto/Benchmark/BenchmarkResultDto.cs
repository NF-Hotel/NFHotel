namespace NFHotel.Application.Dto.Benchmark;

/// <summary>One request of a stored run with its measurements; the answer text is read on its own (ADR-0013).</summary>
/// <param name="BenchmarkResultId">The key of the request, used to read the answer.</param>
/// <param name="Timestamp">When the request was made.</param>
/// <param name="Model">The model.</param>
/// <param name="AnalysisType">The analysis type.</param>
/// <param name="TaskId">The task, or <c>null</c>.</param>
/// <param name="RunNumber">The repetition of the request, from 0.</param>
/// <param name="Cold">True for the first request to the model.</param>
/// <param name="TimeToFirstTokenSeconds">Seconds to the first token, or <c>null</c>.</param>
/// <param name="TotalSeconds">Seconds for the request.</param>
/// <param name="CompletionTokens">Tokens in the answer.</param>
/// <param name="TokensPerSecond">Throughput, or <c>null</c>.</param>
/// <param name="TokensEstimated">True when the token count was estimated.</param>
/// <param name="RamPeakMb">Peak system memory in megabytes, or <c>null</c>.</param>
/// <param name="VramPeakMb">Peak GPU memory in megabytes, or <c>null</c>.</param>
/// <param name="Passed">Whether a deterministic task passed, or <c>null</c>.</param>
/// <param name="ResponseValid">Whether the answer had the required shape, or <c>null</c>.</param>
/// <param name="HasError">True when the request ended with an error.</param>
/// <param name="RamBeforeMb">System memory in use before the request in megabytes, or <c>null</c>.</param>
/// <param name="RamAfterMb">System memory in use after the request in megabytes, or <c>null</c>.</param>
/// <param name="VramBeforeMb">GPU memory in use before the request in megabytes, or <c>null</c>.</param>
/// <param name="VramAfterMb">GPU memory in use after the request in megabytes, or <c>null</c>.</param>
/// <param name="FinishReason">Why the answer ended, or <c>null</c>.</param>
public sealed record BenchmarkResultDto(
    int BenchmarkResultId,
    DateTimeOffset Timestamp,
    string Model,
    string AnalysisType,
    string? TaskId,
    int RunNumber,
    bool Cold,
    double? TimeToFirstTokenSeconds,
    double TotalSeconds,
    int CompletionTokens,
    double? TokensPerSecond,
    bool TokensEstimated,
    double? RamPeakMb,
    double? VramPeakMb,
    bool? Passed,
    bool? ResponseValid,
    bool HasError,
    double? RamBeforeMb = null,
    double? RamAfterMb = null,
    double? VramBeforeMb = null,
    double? VramAfterMb = null,
    string? FinishReason = null);
