using System.Text.Json.Serialization;

namespace NFHotel.Application.Dto.Benchmark;

/// <summary>The machine of a run, as the contract names its fields.</summary>
/// <param name="Cpu">The processor name.</param>
/// <param name="CpuCores">The logical cores.</param>
/// <param name="MemoryMb">The installed memory in megabytes.</param>
/// <param name="Gpu">The GPU name, or <c>null</c> for the CPU only.</param>
/// <param name="Os">The operating system and its release.</param>
public sealed record BenchmarkMachineDto(
    [property: JsonPropertyName("cpu")] string Cpu,
    [property: JsonPropertyName("cpu_cores")] int CpuCores,
    [property: JsonPropertyName("memory_mb")] int MemoryMb,
    [property: JsonPropertyName("gpu")] string? Gpu,
    [property: JsonPropertyName("os")] string Os);