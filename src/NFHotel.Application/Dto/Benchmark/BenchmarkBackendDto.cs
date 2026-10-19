using System.Text.Json.Serialization;

namespace NFHotel.Application.Dto.Benchmark;

/// <summary>The model server of a run, as the contract names its fields.</summary>
/// <param name="Name">The server, for example <c>ollama</c>.</param>
/// <param name="Version">Its version, or <c>null</c>.</param>
public sealed record BenchmarkBackendDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("version")] string? Version);