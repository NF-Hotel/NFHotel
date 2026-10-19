namespace NFHotel.Domain.ValueObjects;

/// <summary>
/// The measurements and the outcome of one request to one model, in the fields of the result schema version 2
/// (ADR-0008). The memory figures are in megabytes; a figure that could not be measured is <c>null</c>.
/// </summary>
/// <param name="Timestamp">When the request was made.</param>
/// <param name="Backend">The model server, for example <c>ollama</c>.</param>
/// <param name="Model">The model name as the server lists it.</param>
/// <param name="AnalysisType">Either <c>hotel_cancellation</c> or <c>deterministic_task</c>.</param>
/// <param name="TaskId">The task, or <c>null</c>.</param>
/// <param name="Category">The task category, or <c>null</c>.</param>
/// <param name="RunNumber">The repetition of the request, from 0.</param>
/// <param name="Cold">True for the first request to a model of a session.</param>
/// <param name="TimeToFirstTokenSeconds">Time to the first token, or <c>null</c>.</param>
/// <param name="TotalSeconds">Total time of the request.</param>
/// <param name="CompletionTokens">Tokens in the answer.</param>
/// <param name="PromptTokens">Tokens in the prompt, or <c>null</c>.</param>
/// <param name="TokensPerSecond">Throughput, or <c>null</c>.</param>
/// <param name="TokensEstimated">True when the server gave no token count.</param>
/// <param name="FinishReason">Why the answer ended, or <c>null</c>.</param>
/// <param name="RamBeforeMb">System memory in use before the request, or <c>null</c>.</param>
/// <param name="RamAfterMb">System memory in use after the request, or <c>null</c>.</param>
/// <param name="RamPeakMb">Peak system memory in use, or <c>null</c>.</param>
/// <param name="VramBeforeMb">GPU memory in use before the request, or <c>null</c>.</param>
/// <param name="VramAfterMb">GPU memory in use after the request, or <c>null</c>.</param>
/// <param name="VramPeakMb">Peak GPU memory in use, or <c>null</c>.</param>
/// <param name="Passed">Whether a deterministic task passed, or <c>null</c>.</param>
/// <param name="ResponseValid">Whether the answer had the required JSON shape, or <c>null</c>.</param>
/// <param name="ValidationError">Why the answer was invalid, or <c>null</c>.</param>
/// <param name="Output">The answer text, or <c>null</c>.</param>
/// <param name="Error">The error of the request, or <c>null</c>.</param>
public sealed record BenchmarkResultData(
    DateTimeOffset Timestamp,
    string Backend,
    string Model,
    string AnalysisType,
    string? TaskId,
    string? Category,
    int RunNumber,
    bool Cold,
    double? TimeToFirstTokenSeconds,
    double TotalSeconds,
    int CompletionTokens,
    int? PromptTokens,
    double? TokensPerSecond,
    bool TokensEstimated,
    string? FinishReason,
    double? RamBeforeMb,
    double? RamAfterMb,
    double? RamPeakMb,
    double? VramBeforeMb,
    double? VramAfterMb,
    double? VramPeakMb,
    bool? Passed,
    bool? ResponseValid,
    string? ValidationError,
    string? Output,
    string? Error);