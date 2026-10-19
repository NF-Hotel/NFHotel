namespace NFHotel.Domain.Errors;

/// <summary>
/// Error codes of the benchmark aggregates (ADR-0013). A code is a stable string; the text for the user is the
/// concern of the presentation layer.
/// </summary>
public static class BenchmarkErrors
{
    /// <summary>The run id is missing or is the empty id.</summary>
    public const string RunIdRequired = "benchmark.run_id_required";

    /// <summary>The run ended before it started.</summary>
    public const string EndBeforeStart = "benchmark.end_before_start";

    /// <summary>The dataset name is missing or too long.</summary>
    public const string DatasetNameInvalid = "benchmark.dataset_name_invalid";

    /// <summary>The dataset hash is not a SHA-256 in lower-case hexadecimal.</summary>
    public const string DatasetHashInvalid = "benchmark.dataset_hash_invalid";

    /// <summary>The dataset has a negative number of rows.</summary>
    public const string DatasetRowsInvalid = "benchmark.dataset_rows_invalid";

    /// <summary>The prompt version is missing or too long.</summary>
    public const string PromptVersionInvalid = "benchmark.prompt_version_invalid";

    /// <summary>A part of the machine description is missing, too long or out of range.</summary>
    public const string MachineInvalid = "benchmark.machine_invalid";

    /// <summary>The backend name is missing or too long.</summary>
    public const string BackendInvalid = "benchmark.backend_invalid";

    /// <summary>A run has no request result.</summary>
    public const string ResultsRequired = "benchmark.results_required";

    /// <summary>The model name of a request is missing or too long.</summary>
    public const string ModelInvalid = "benchmark.model_invalid";

    /// <summary>The analysis type of a request is not a known one.</summary>
    public const string AnalysisTypeInvalid = "benchmark.analysis_type_invalid";

    /// <summary>A time, a token count or a memory figure of a request is negative.</summary>
    public const string MeasurementInvalid = "benchmark.measurement_invalid";

    /// <summary>The number of the run of a request is negative.</summary>
    public const string RunNumberInvalid = "benchmark.run_number_invalid";
}
