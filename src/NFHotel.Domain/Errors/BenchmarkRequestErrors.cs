namespace NFHotel.Domain.Errors;

/// <summary>
/// Error codes of the record of a started benchmark run (ADR-0031). A code is a stable string; the text for the user is
/// the concern of the presentation layer.
/// </summary>
public static class BenchmarkRequestErrors
{
    /// <summary>The run id is missing.</summary>
    public const string RunIdRequired = "benchmark_request.run_id_required";

    /// <summary>No model, more than the limit, a model twice, or a name that is empty or too long.</summary>
    public const string ModelsInvalid = "benchmark_request.models_invalid";

    /// <summary>Neither the hotel analysis nor the deterministic tasks were chosen.</summary>
    public const string TasksInvalid = "benchmark_request.tasks_invalid";

    /// <summary>The repetitions per model and task are out of range.</summary>
    public const string RunsInvalid = "benchmark_request.runs_invalid";

    /// <summary>The name of the staff member who started the run is missing or too long.</summary>
    public const string RequestedByInvalid = "benchmark_request.requested_by_invalid";

    /// <summary>The state is not one of the known ones.</summary>
    public const string StateInvalid = "benchmark_request.state_invalid";

    /// <summary>The run cannot go from its state to the given one, for example from done back to running.</summary>
    public const string StateChangeRefused = "benchmark_request.state_change_refused";

    /// <summary>The error text is too long.</summary>
    public const string ErrorInvalid = "benchmark_request.error_invalid";
}
