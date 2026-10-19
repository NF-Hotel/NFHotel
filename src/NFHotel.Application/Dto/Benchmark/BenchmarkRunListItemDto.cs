namespace NFHotel.Application.Dto.Benchmark;

/// <summary>A stored run as the list shows it: no answer text, only what is needed to pick a run.</summary>
/// <param name="RunId">The id of the run.</param>
/// <param name="StartedAt">When the run started.</param>
/// <param name="FinishedAt">When the run ended.</param>
/// <param name="DatasetName">The dataset.</param>
/// <param name="Cpu">The processor.</param>
/// <param name="MemoryMb">The installed memory in megabytes.</param>
/// <param name="Gpu">The GPU, or <c>null</c> for the CPU only.</param>
/// <param name="BackendName">The model server.</param>
/// <param name="BackendVersion">Its version, or <c>null</c>.</param>
/// <param name="Models">The distinct models of the run, in alphabetical order.</param>
/// <param name="RequestCount">The number of requests in the run.</param>
public sealed record BenchmarkRunListItemDto(
    Guid RunId,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    string DatasetName,
    string Cpu,
    int MemoryMb,
    string? Gpu,
    string BackendName,
    string? BackendVersion,
    IReadOnlyList<string> Models,
    int RequestCount);