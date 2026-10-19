namespace NFHotel.Domain.ValueObjects;

/// <summary>The machine a benchmark run was made on, so that a run on the CPU is not mistaken for a run on a GPU.</summary>
/// <param name="Cpu">The processor name.</param>
/// <param name="CpuCores">The number of logical cores.</param>
/// <param name="MemoryMb">The installed memory in megabytes.</param>
/// <param name="Gpu">The GPU name, or <c>null</c> when the run used the CPU only.</param>
/// <param name="OperatingSystem">The operating system and its release.</param>
public sealed record BenchmarkMachine(string Cpu, int CpuCores, int MemoryMb, string? Gpu, string OperatingSystem);
