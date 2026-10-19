using System.Text.Json.Serialization;

namespace NFHotel.Application.Dto.Benchmark;

/// <summary>
/// The body that the benchmark service posts when a run is ready: the <c>RunResult</c> of the contract
/// <c>openapi/benchmark-contract.yaml</c> (ADR-0013). The field names are those of the contract.
/// </summary>
/// <param name="SchemaVersion">The result schema; only 2 is accepted.</param>
/// <param name="RunId">The id of the run; the key of idempotency.</param>
/// <param name="StartedAt">When the run started.</param>
/// <param name="FinishedAt">When the run ended.</param>
/// <param name="PromptVersion">The version of the prompt.</param>
/// <param name="Dataset">The dataset.</param>
/// <param name="Machine">The machine.</param>
/// <param name="Backend">The model server.</param>
/// <param name="Results">One record per request.</param>
public sealed record SubmitBenchmarkRunRequest(
    [property: JsonPropertyName("schema_version")] int SchemaVersion,
    [property: JsonPropertyName("run_id")] Guid RunId,
    [property: JsonPropertyName("started_at")] DateTimeOffset StartedAt,
    [property: JsonPropertyName("finished_at")] DateTimeOffset FinishedAt,
    [property: JsonPropertyName("prompt_version")] string PromptVersion,
    [property: JsonPropertyName("dataset")] BenchmarkDatasetDto Dataset,
    [property: JsonPropertyName("machine")] BenchmarkMachineDto Machine,
    [property: JsonPropertyName("backend")] BenchmarkBackendDto Backend,
    [property: JsonPropertyName("results")] IReadOnlyList<BenchmarkRequestResultDto> Results);
