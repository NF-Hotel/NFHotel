using System.ComponentModel.DataAnnotations;

using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Entities;

/// <summary>
/// A finished benchmark run as the benchmark service delivered it: the header of the run and one
/// <see cref="BenchmarkResult"/> per request (ADR-0013). The run id of the contract is stored once.
/// </summary>
public sealed class BenchmarkRun : Entity<int>
{
    /// <summary>The longest dataset name, prompt version, backend name, machine text and version.</summary>
    public const int MaxTextLength = 200;

    private const string Sha256Pattern = "^[0-9a-f]{64}$";

    private readonly List<BenchmarkResult> _results = [];

    private BenchmarkRun()
    {
    }

    /// <summary>The id the benchmark service gave the run; the key of idempotency.</summary>
    public Guid RunId { get; private set; }

    /// <summary>When the run started.</summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>When the run ended.</summary>
    public DateTimeOffset FinishedAt { get; private set; }

    /// <summary>The dataset name.</summary>
    [Required(ErrorMessage = BenchmarkErrors.DatasetNameInvalid)]
    [StringLength(MaxTextLength, ErrorMessage = BenchmarkErrors.DatasetNameInvalid)]
    public string DatasetName { get; private set; } = string.Empty;

    /// <summary>The SHA-256 of the dataset file in lower-case hexadecimal.</summary>
    [Required(ErrorMessage = BenchmarkErrors.DatasetHashInvalid)]
    [RegularExpression(Sha256Pattern, ErrorMessage = BenchmarkErrors.DatasetHashInvalid)]
    public string DatasetHash { get; private set; } = string.Empty;

    /// <summary>The number of rows of the dataset.</summary>
    [Range(0, int.MaxValue, ErrorMessage = BenchmarkErrors.DatasetRowsInvalid)]
    public int DatasetRows { get; private set; }

    /// <summary>The version of the prompt.</summary>
    [Required(ErrorMessage = BenchmarkErrors.PromptVersionInvalid)]
    [StringLength(MaxTextLength, ErrorMessage = BenchmarkErrors.PromptVersionInvalid)]
    public string PromptVersion { get; private set; } = string.Empty;

    /// <summary>The processor name.</summary>
    [Required(ErrorMessage = BenchmarkErrors.MachineInvalid)]
    [StringLength(MaxTextLength, ErrorMessage = BenchmarkErrors.MachineInvalid)]
    public string Cpu { get; private set; } = string.Empty;

    /// <summary>The number of logical cores.</summary>
    [Range(1, int.MaxValue, ErrorMessage = BenchmarkErrors.MachineInvalid)]
    public int CpuCores { get; private set; }

    /// <summary>The installed memory in megabytes.</summary>
    [Range(1, int.MaxValue, ErrorMessage = BenchmarkErrors.MachineInvalid)]
    public int MemoryMb { get; private set; }

    /// <summary>The GPU name, or <c>null</c> when the run used the CPU only.</summary>
    [StringLength(MaxTextLength, ErrorMessage = BenchmarkErrors.MachineInvalid)]
    public string? Gpu { get; private set; }

    /// <summary>The operating system and its release.</summary>
    [Required(ErrorMessage = BenchmarkErrors.MachineInvalid)]
    [StringLength(MaxTextLength, ErrorMessage = BenchmarkErrors.MachineInvalid)]
    public string OperatingSystem { get; private set; } = string.Empty;

    /// <summary>The model server.</summary>
    [Required(ErrorMessage = BenchmarkErrors.BackendInvalid)]
    [StringLength(MaxTextLength, ErrorMessage = BenchmarkErrors.BackendInvalid)]
    public string BackendName { get; private set; } = string.Empty;

    /// <summary>The version of the model server, or <c>null</c> when it could not be read.</summary>
    [StringLength(MaxTextLength, ErrorMessage = BenchmarkErrors.BackendInvalid)]
    public string? BackendVersion { get; private set; }

    /// <summary>The requests of the run, in the order they were delivered.</summary>
    public IReadOnlyList<BenchmarkResult> Results => _results;

    /// <summary>Creates a run with its results and checks every field.</summary>
    /// <param name="runId">The id from the benchmark service.</param>
    /// <param name="startedAt">When the run started.</param>
    /// <param name="finishedAt">When the run ended.</param>
    /// <param name="datasetName">The dataset name.</param>
    /// <param name="datasetHash">The SHA-256 of the dataset.</param>
    /// <param name="datasetRows">The rows of the dataset.</param>
    /// <param name="promptVersion">The version of the prompt.</param>
    /// <param name="machine">The machine of the run.</param>
    /// <param name="backendName">The model server.</param>
    /// <param name="backendVersion">The version of the model server, or <c>null</c>.</param>
    /// <param name="results">The requests; at least one.</param>
    /// <returns>The new run.</returns>
    /// <exception cref="DomainException">A field breaks an invariant.</exception>
    public static BenchmarkRun Create(
        Guid runId,
        DateTimeOffset startedAt,
        DateTimeOffset finishedAt,
        string datasetName,
        string datasetHash,
        int datasetRows,
        string promptVersion,
        BenchmarkMachine machine,
        string backendName,
        string? backendVersion,
        IReadOnlyList<BenchmarkResultData> results)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(results);

        var codes = new List<string>();
        if (runId == Guid.Empty)
        {
            codes.Add(BenchmarkErrors.RunIdRequired);
        }

        if (finishedAt < startedAt)
        {
            codes.Add(BenchmarkErrors.EndBeforeStart);
        }

        if (results.Count == 0)
        {
            codes.Add(BenchmarkErrors.ResultsRequired);
        }

        var run = new BenchmarkRun
        {
            RunId = runId,
            StartedAt = startedAt.ToUniversalTime(),
            FinishedAt = finishedAt.ToUniversalTime(),
            DatasetName = datasetName ?? string.Empty,
            DatasetHash = datasetHash ?? string.Empty,
            DatasetRows = datasetRows,
            PromptVersion = promptVersion ?? string.Empty,
            Cpu = machine.Cpu ?? string.Empty,
            CpuCores = machine.CpuCores,
            MemoryMb = machine.MemoryMb,
            Gpu = machine.Gpu,
            OperatingSystem = machine.OperatingSystem ?? string.Empty,
            BackendName = backendName ?? string.Empty,
            BackendVersion = backendVersion,
        };

        try
        {
            EntityValidation.ThrowIfInvalid(run);
        }
        catch (DomainException exception)
        {
            codes.AddRange(exception.Codes);
        }

        foreach (var data in results)
        {
            try
            {
                run._results.Add(BenchmarkResult.Create(data));
            }
            catch (DomainException exception)
            {
                codes.AddRange(exception.Codes);
            }
        }

        if (codes.Count > 0)
        {
            throw new DomainException(codes.Distinct().ToArray());
        }

        return run;
    }
}
