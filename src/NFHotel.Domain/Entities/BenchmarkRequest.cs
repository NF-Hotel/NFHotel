using System.ComponentModel.DataAnnotations;

using NFHotel.Domain.Errors;

namespace NFHotel.Domain.Entities;

/// <summary>
/// A benchmark run that a Manager started, as the hotel records it (ADR-0031): the choice, who started it and when, and
/// the last state the benchmark service reported. The service keeps its runs in memory, so this record is what is left
/// when it restarts: the run then becomes <see cref="Interrupted"/>. The result of a finished run is a
/// <see cref="BenchmarkRun"/>.
/// </summary>
public sealed class BenchmarkRequest : Entity<int>
{
    /// <summary>The service has the run in its queue.</summary>
    public const string Queued = "queued";

    /// <summary>The service runs the models.</summary>
    public const string Running = "running";

    /// <summary>The service finished the run; its result is delivered as a <see cref="BenchmarkRun"/>.</summary>
    public const string Done = "done";

    /// <summary>The service gave up on the run.</summary>
    public const string Failed = "failed";

    /// <summary>The service no longer knows the run: it was restarted before the run ended.</summary>
    public const string Interrupted = "interrupted";

    /// <summary>The most models in one run, as the benchmark service allows.</summary>
    public const int MaxModels = 4;

    /// <summary>The most repetitions per model and task, as the benchmark service allows.</summary>
    public const int MaxRuns = 5;

    /// <summary>The longest model name and user name.</summary>
    public const int MaxTextLength = 100;

    /// <summary>The longest error text that is kept.</summary>
    public const int MaxErrorLength = 1000;

    /// <summary>The known states.</summary>
    public static readonly IReadOnlyList<string> States = [Queued, Running, Done, Failed, Interrupted];

    // The states each state may change to; a final state changes to nothing.
    private static readonly Dictionary<string, string[]> NextStates = new()
    {
        [Queued] = [Running, Done, Failed, Interrupted],
        [Running] = [Done, Failed, Interrupted],
        [Done] = [],
        [Failed] = [],
        [Interrupted] = [],
    };

    private List<string> _models = [];

    private BenchmarkRequest()
    {
    }

    /// <summary>The id the benchmark service gave the run.</summary>
    public Guid RunId { get; private set; }

    /// <summary>The chosen models, in the order they were chosen.</summary>
    public IReadOnlyList<string> Models => _models;

    /// <summary>True when the hotel cancellation analysis was chosen.</summary>
    public bool HotelAnalysis { get; private set; }

    /// <summary>True when the deterministic tasks were chosen.</summary>
    public bool DeterministicTasks { get; private set; }

    /// <summary>Requests per model and task.</summary>
    [Range(1, MaxRuns, ErrorMessage = BenchmarkRequestErrors.RunsInvalid)]
    public int Runs { get; private set; }

    /// <summary>The user name of the staff member who started the run.</summary>
    [Required(ErrorMessage = BenchmarkRequestErrors.RequestedByInvalid)]
    [StringLength(MaxTextLength, ErrorMessage = BenchmarkRequestErrors.RequestedByInvalid)]
    public string RequestedBy { get; private set; } = string.Empty;

    /// <summary>When the run was started.</summary>
    public DateTimeOffset RequestedAt { get; private set; }

    /// <summary>The last state: one of <see cref="States"/>.</summary>
    public string State { get; private set; } = Queued;

    /// <summary>Why the run failed or was interrupted, or <c>null</c>.</summary>
    [StringLength(MaxErrorLength, ErrorMessage = BenchmarkRequestErrors.ErrorInvalid)]
    public string? Error { get; private set; }

    /// <summary>When the state was last changed.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>True when the state will not change any more.</summary>
    public bool IsFinal => NextStates[State].Length == 0;

    /// <summary>Records a run that the benchmark service has just accepted; it starts as <see cref="Queued"/>.</summary>
    /// <param name="runId">The id the service gave the run.</param>
    /// <param name="models">The chosen models.</param>
    /// <param name="hotelAnalysis">True for the hotel cancellation analysis.</param>
    /// <param name="deterministicTasks">True for the deterministic tasks.</param>
    /// <param name="runs">Requests per model and task.</param>
    /// <param name="requestedBy">Who started it.</param>
    /// <param name="requestedAt">When it was started.</param>
    /// <returns>The new record.</returns>
    /// <exception cref="DomainException">A field breaks an invariant.</exception>
    public static BenchmarkRequest Create(
        Guid runId,
        IEnumerable<string> models,
        bool hotelAnalysis,
        bool deterministicTasks,
        int runs,
        string requestedBy,
        DateTimeOffset requestedAt)
    {
        // Plain loops, not lambdas: an entity file holds no compiler-made closure types (StructureTests).
        var trimmed = new List<string>();
        foreach (var model in models ?? [])
        {
            trimmed.Add(model?.Trim() ?? string.Empty);
        }

        var request = new BenchmarkRequest
        {
            RunId = runId,
            _models = trimmed,
            HotelAnalysis = hotelAnalysis,
            DeterministicTasks = deterministicTasks,
            Runs = runs,
            RequestedBy = requestedBy?.Trim() ?? string.Empty,
            RequestedAt = requestedAt.ToUniversalTime(),
            UpdatedAt = requestedAt.ToUniversalTime(),
        };

        var codes = new List<string>();
        if (runId == Guid.Empty)
        {
            codes.Add(BenchmarkRequestErrors.RunIdRequired);
        }

        if (!AreValidModels(trimmed))
        {
            codes.Add(BenchmarkRequestErrors.ModelsInvalid);
        }

        if (!hotelAnalysis && !deterministicTasks)
        {
            codes.Add(BenchmarkRequestErrors.TasksInvalid);
        }

        try
        {
            EntityValidation.ThrowIfInvalid(request);
        }
        catch (DomainException exception)
        {
            codes.AddRange(exception.Codes);
        }

        if (codes.Count > 0)
        {
            throw new DomainException(codes.Distinct().ToArray());
        }

        return request;
    }

    private static bool AreValidModels(List<string> models)
    {
        if (models.Count is 0 or > MaxModels)
        {
            return false;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var model in models)
        {
            if (model.Length is 0 or > MaxTextLength || !seen.Add(model))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Records the state the service reported. The same state again changes nothing; a state that the current one may not
    /// change to is refused, so a late answer never turns a finished run back into a running one.
    /// </summary>
    /// <param name="state">The new state.</param>
    /// <param name="error">Why the run failed or was interrupted, or <c>null</c>.</param>
    /// <param name="at">When the state was seen.</param>
    /// <returns>True when the record changed.</returns>
    /// <exception cref="DomainException">The state is unknown, the change is refused, or the error is too long.</exception>
    public bool ChangeState(string state, string? error, DateTimeOffset at)
    {
        if (state is null || !States.Contains(state))
        {
            throw new DomainException(BenchmarkRequestErrors.StateInvalid);
        }

        if (state == State)
        {
            return false;
        }

        if (!NextStates[State].Contains(state))
        {
            throw new DomainException(BenchmarkRequestErrors.StateChangeRefused);
        }

        var text = string.IsNullOrWhiteSpace(error) ? null : error.Trim();
        if (text is { Length: > MaxErrorLength })
        {
            throw new DomainException(BenchmarkRequestErrors.ErrorInvalid);
        }

        State = state;
        Error = text;
        UpdatedAt = at.ToUniversalTime();
        return true;
    }
}
