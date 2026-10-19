using System.Text.Json.Serialization;

namespace NFHotel.Application.Dto.Benchmark;

/// <summary>
/// One request to one model, with exactly the field names of the result schema version 2 (the notebook and the
/// contract file <c>openapi/benchmark-contract.yaml</c>). Memory figures are in megabytes.
/// </summary>
/// <param name="SchemaVersion">The version of the record; ignored here, the run carries the checked one.</param>
/// <param name="Timestamp">When the request was made.</param>
/// <param name="SessionId">The session of the notebook or service; not stored.</param>
/// <param name="Backend">The model server.</param>
/// <param name="Model">The model name.</param>
/// <param name="TaskId">The task, or <c>null</c>.</param>
/// <param name="Category">The task category, or <c>null</c>.</param>
/// <param name="AnalysisType">Either <c>hotel_cancellation</c> or <c>deterministic_task</c>.</param>
/// <param name="Run">The repetition of the request, from 0.</param>
/// <param name="Cold">True for the first request to a model of a session.</param>
/// <param name="TtftSeconds">Time to the first token, or <c>null</c>.</param>
/// <param name="TotalSeconds">Total time of the request.</param>
/// <param name="CompletionTokens">Tokens in the answer.</param>
/// <param name="PromptTokens">Tokens in the prompt, or <c>null</c>.</param>
/// <param name="TokensPerSecond">Throughput, or <c>null</c>.</param>
/// <param name="TokensEstimated">True when the server gave no token count.</param>
/// <param name="FinishReason">Why the answer ended, or <c>null</c>.</param>
/// <param name="SystemRamBeforeMb">System memory in use before the request, or <c>null</c>.</param>
/// <param name="SystemRamAfterMb">System memory in use after the request, or <c>null</c>.</param>
/// <param name="SystemRamPeakMb">Peak system memory in use, or <c>null</c>.</param>
/// <param name="GpuVramBeforeMb">GPU memory in use before the request, or <c>null</c>.</param>
/// <param name="GpuVramAfterMb">GPU memory in use after the request, or <c>null</c>.</param>
/// <param name="GpuVramPeakMb">Peak GPU memory in use, or <c>null</c>.</param>
/// <param name="Passed">Whether a deterministic task passed, or <c>null</c>.</param>
/// <param name="ResponseValid">Whether the answer had the required JSON shape, or <c>null</c>.</param>
/// <param name="ValidationError">Why the answer was invalid, or <c>null</c>.</param>
/// <param name="Output">The answer text, or <c>null</c>.</param>
/// <param name="Error">The error of the request, or <c>null</c>.</param>
public sealed record BenchmarkRequestResultDto(
    [property: JsonPropertyName("schema_version")] int? SchemaVersion,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("session_id")] string? SessionId,
    [property: JsonPropertyName("backend")] string Backend,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("task_id")] string? TaskId,
    [property: JsonPropertyName("category")] string? Category,
    [property: JsonPropertyName("analysis_type")] string AnalysisType,
    [property: JsonPropertyName("run")] int Run,
    [property: JsonPropertyName("cold")] bool Cold,
    [property: JsonPropertyName("ttft_s")] double? TtftSeconds,
    [property: JsonPropertyName("total_s")] double TotalSeconds,
    [property: JsonPropertyName("completion_tokens")] int CompletionTokens,
    [property: JsonPropertyName("prompt_tokens")] int? PromptTokens,
    [property: JsonPropertyName("tokens_per_s")] double? TokensPerSecond,
    [property: JsonPropertyName("tokens_estimated")] bool TokensEstimated,
    [property: JsonPropertyName("finish_reason")] string? FinishReason,
    [property: JsonPropertyName("system_ram_before_mb")] double? SystemRamBeforeMb,
    [property: JsonPropertyName("system_ram_after_mb")] double? SystemRamAfterMb,
    [property: JsonPropertyName("system_ram_peak_mb")] double? SystemRamPeakMb,
    [property: JsonPropertyName("gpu_vram_before_mb")] double? GpuVramBeforeMb,
    [property: JsonPropertyName("gpu_vram_after_mb")] double? GpuVramAfterMb,
    [property: JsonPropertyName("gpu_vram_peak_mb")] double? GpuVramPeakMb,
    [property: JsonPropertyName("passed")] bool? Passed,
    [property: JsonPropertyName("response_valid")] bool? ResponseValid,
    [property: JsonPropertyName("validation_error")] string? ValidationError,
    [property: JsonPropertyName("output")] string? Output,
    [property: JsonPropertyName("error")] string? Error);