using System.Text.Json.Serialization;

namespace NFHotel.Application.Dto.Benchmark;

/// <summary>The dataset of a run, as the contract names its fields.</summary>
/// <param name="Name">The file name.</param>
/// <param name="Sha256">The SHA-256 of the file in lower-case hexadecimal.</param>
/// <param name="Rows">The number of rows.</param>
public sealed record BenchmarkDatasetDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("sha256")] string Sha256,
    [property: JsonPropertyName("rows")] int Rows);