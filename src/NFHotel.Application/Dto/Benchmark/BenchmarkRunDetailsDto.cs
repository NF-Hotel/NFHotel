namespace NFHotel.Application.Dto.Benchmark;

/// <summary>A stored run with every request and its measurements.</summary>
/// <param name="Header">The header of the run, as in the list.</param>
/// <param name="DatasetHash">The SHA-256 of the dataset.</param>
/// <param name="DatasetRows">The rows of the dataset.</param>
/// <param name="PromptVersion">The version of the prompt.</param>
/// <param name="CpuCores">The logical cores of the machine.</param>
/// <param name="OperatingSystem">The operating system of the machine.</param>
/// <param name="Results">The requests in the order they were delivered.</param>
public sealed record BenchmarkRunDetailsDto(
    BenchmarkRunListItemDto Header,
    string DatasetHash,
    int DatasetRows,
    string PromptVersion,
    int CpuCores,
    string OperatingSystem,
    IReadOnlyList<BenchmarkResultDto> Results);
