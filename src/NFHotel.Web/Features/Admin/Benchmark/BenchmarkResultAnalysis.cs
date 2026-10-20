using NFHotel.Application.Dto.Benchmark;

namespace NFHotel.Web.Features.Admin.Benchmark;

/// <summary>Which requests of a run an analysis looks at.</summary>
public enum BenchmarkWorkload
{
    /// <summary>The hotel cancellation analysis.</summary>
    Hotel,

    /// <summary>The deterministic tasks.</summary>
    Deterministic,

    /// <summary>Both.</summary>
    Both,
}

/// <summary>The mean figures of one model in one phase (cold is the first request of a model, warm the later ones).</summary>
/// <param name="Model">The model name.</param>
/// <param name="Phase">Cold or warm.</param>
/// <param name="FirstTokenSeconds">Mean time to the first token, or <c>null</c> when no request measured it.</param>
/// <param name="TotalSeconds">Mean total time.</param>
/// <param name="CompletionTokens">Mean number of tokens of the answer.</param>
/// <param name="TokensPerSecond">Mean speed, or <c>null</c> when no request measured it.</param>
/// <param name="Requests">How many requests the means are over.</param>
public sealed record PhaseStats(
    string Model,
    string Phase,
    double? FirstTokenSeconds,
    double TotalSeconds,
    double CompletionTokens,
    double? TokensPerSecond,
    int Requests);

/// <summary>How a model did on one deterministic task over all its runs.</summary>
/// <param name="Model">The model name.</param>
/// <param name="TaskId">The task.</param>
/// <param name="Passed">How many runs passed.</param>
/// <param name="Total">How many runs were graded.</param>
public sealed record TaskOutcome(string Model, string TaskId, int Passed, int Total)
{
    /// <summary>True when every graded run passed.</summary>
    public bool AllPassed => Total > 0 && Passed == Total;
}

/// <summary>The mean memory of one model before a request, after it and at its peak.</summary>
/// <param name="Model">The model name.</param>
/// <param name="RamBeforeMb">System memory before.</param>
/// <param name="RamAfterMb">System memory after.</param>
/// <param name="RamPeakMb">System memory at the peak.</param>
/// <param name="VramBeforeMb">GPU memory before.</param>
/// <param name="VramAfterMb">GPU memory after.</param>
/// <param name="VramPeakMb">GPU memory at the peak.</param>
public sealed record ResourceUsage(
    string Model,
    double? RamBeforeMb,
    double? RamAfterMb,
    double? RamPeakMb,
    double? VramBeforeMb,
    double? VramAfterMb,
    double? VramPeakMb);

/// <summary>
/// The numbers behind the performance view of one stored benchmark run (the view of the notebook of the benchmark
/// repository): means per model and phase, task outcomes and memory. A request that ended with an error is left out of
/// every figure, as the notebook does. The page holds only markup and binding.
/// </summary>
public static class BenchmarkResultAnalysis
{
    /// <summary>The analysis type of the hotel cancellation analysis in the stored results.</summary>
    public const string HotelAnalysisType = "hotel_cancellation";

    /// <summary>The phase of the first request of a model.</summary>
    public const string Cold = "cold";

    /// <summary>The phase of the later requests of a model.</summary>
    public const string Warm = "warm";

    /// <summary>Keeps the requests of a workload.</summary>
    /// <param name="results">The requests of a run.</param>
    /// <param name="workload">The workload to keep.</param>
    /// <returns>The requests of the workload, in their order.</returns>
    public static IReadOnlyList<BenchmarkResultDto> Select(IEnumerable<BenchmarkResultDto> results, BenchmarkWorkload workload)
    {
        ArgumentNullException.ThrowIfNull(results);

        return
        [
            .. results.Where(result => workload switch
            {
                BenchmarkWorkload.Hotel => IsHotel(result),
                BenchmarkWorkload.Deterministic => !IsHotel(result),
                _ => true,
            }),
        ];
    }

    /// <summary>The label of a task: the hotel cancellation analysis or the task id.</summary>
    /// <param name="result">The request.</param>
    /// <returns>The text to show.</returns>
    public static string TaskName(BenchmarkResultDto result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return IsHotel(result) ? "Hotel cancellation analysis" : result.TaskId ?? result.AnalysisType;
    }

    /// <summary>Tells whether a request is the hotel cancellation analysis.</summary>
    /// <param name="result">The request.</param>
    /// <returns>True for the hotel cancellation analysis.</returns>
    public static bool IsHotel(BenchmarkResultDto result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return string.Equals(result.AnalysisType, HotelAnalysisType, StringComparison.Ordinal);
    }

    /// <summary>The mean figures per model and phase of the requests that did not fail.</summary>
    /// <param name="results">The requests.</param>
    /// <returns>One row per model and phase, ordered by model and then cold before warm.</returns>
    public static IReadOnlyList<PhaseStats> Phases(IEnumerable<BenchmarkResultDto> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return
        [
            .. results
                .Where(result => !result.HasError)
                .GroupBy(result => (result.Model, Phase: result.Cold ? Cold : Warm))
                .Select(group => new PhaseStats(
                    group.Key.Model,
                    group.Key.Phase,
                    MeanOrNull(group.Select(result => result.TimeToFirstTokenSeconds)),
                    group.Average(result => result.TotalSeconds),
                    group.Average(result => (double)result.CompletionTokens),
                    MeanOrNull(group.Select(result => result.TokensPerSecond)),
                    group.Count()))
                .OrderBy(stats => stats.Model, StringComparer.Ordinal)
                .ThenBy(stats => stats.Phase == Cold ? 0 : 1),
        ];
    }

    /// <summary>The outcome per model and deterministic task, over the runs that were graded.</summary>
    /// <param name="results">The requests.</param>
    /// <returns>One row per model and task, ordered by model and task.</returns>
    public static IReadOnlyList<TaskOutcome> TaskOutcomes(IEnumerable<BenchmarkResultDto> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return
        [
            .. results
                .Where(result => !result.HasError && result.Passed is not null && !IsHotel(result))
                .GroupBy(result => (result.Model, Task: result.TaskId ?? result.AnalysisType))
                .Select(group => new TaskOutcome(group.Key.Model, group.Key.Task, group.Count(result => result.Passed == true), group.Count()))
                .OrderBy(outcome => outcome.Model, StringComparer.Ordinal)
                .ThenBy(outcome => outcome.TaskId, StringComparer.Ordinal),
        ];
    }

    /// <summary>The mean memory per model of the requests that did not fail.</summary>
    /// <param name="results">The requests.</param>
    /// <returns>One row per model, ordered by model.</returns>
    public static IReadOnlyList<ResourceUsage> Resources(IEnumerable<BenchmarkResultDto> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return
        [
            .. results
                .Where(result => !result.HasError)
                .GroupBy(result => result.Model)
                .Select(group => new ResourceUsage(
                    group.Key,
                    MeanOrNull(group.Select(result => result.RamBeforeMb)),
                    MeanOrNull(group.Select(result => result.RamAfterMb)),
                    MeanOrNull(group.Select(result => result.RamPeakMb)),
                    MeanOrNull(group.Select(result => result.VramBeforeMb)),
                    MeanOrNull(group.Select(result => result.VramAfterMb)),
                    MeanOrNull(group.Select(result => result.VramPeakMb))))
                .OrderBy(usage => usage.Model, StringComparer.Ordinal),
        ];
    }

    private static double? MeanOrNull(IEnumerable<double?> values)
    {
        var present = values.Where(value => value is not null).Select(value => value!.Value).ToArray();

        return present.Length == 0 ? null : present.Average();
    }
}
