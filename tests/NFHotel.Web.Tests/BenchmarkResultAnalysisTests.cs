using NFHotel.Application.Dto.Benchmark;
using NFHotel.Web.Features.Admin.Benchmark;

namespace NFHotel.Web.Tests;

/// <summary>The numbers behind the performance view of a stored run: means, task outcomes and memory.</summary>
public sealed class BenchmarkResultAnalysisTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);
    private static int _next;

    #region Workload

    /// <summary>Positive: each workload keeps its own requests and Both keeps all.</summary>
    [Fact]
    public void Select_EachWorkload_KeepsItsRequests()
    {
        // Arrange
        var results = new[] { Hotel("m1"), Task("m1", "math-1", true), Task("m1", "code-1", false) };

        // Act and assert
        Assert.Single(BenchmarkResultAnalysis.Select(results, BenchmarkWorkload.Hotel));
        Assert.Equal(2, BenchmarkResultAnalysis.Select(results, BenchmarkWorkload.Deterministic).Count);
        Assert.Equal(3, BenchmarkResultAnalysis.Select(results, BenchmarkWorkload.Both).Count);
    }

    /// <summary>Positive: the task is named by the analysis or by the task id.</summary>
    [Fact]
    public void TaskName_HotelAndTask_NamesEach()
    {
        // Arrange and act and assert
        Assert.Equal("Hotel cancellation analysis", BenchmarkResultAnalysis.TaskName(Hotel("m1")));
        Assert.Equal("math-1", BenchmarkResultAnalysis.TaskName(Task("m1", "math-1", true)));
    }

    #endregion

    #region Phases

    /// <summary>Positive: the means are per model and phase, cold before warm, and a failed request is left out.</summary>
    [Fact]
    public void Phases_ColdWarmAndAFailedRequest_AveragesPerModelAndPhaseWithoutTheFailure()
    {
        // Arrange
        var results = new[]
        {
            Hotel("m1", cold: true, first: 10, total: 20, tokens: 100, speed: 5),
            Hotel("m1", cold: false, first: 1, total: 4, tokens: 100, speed: 25),
            Hotel("m1", cold: false, first: 3, total: 6, tokens: 200, speed: 33),
            Hotel("m1", cold: false, first: 999, total: 999, tokens: 999, speed: 999, hasError: true),
            Hotel("a1", cold: true, first: null, total: 2, tokens: 10, speed: null),
        };

        // Act
        var phases = BenchmarkResultAnalysis.Phases(results);

        // Assert
        Assert.Equal([("a1", "cold"), ("m1", "cold"), ("m1", "warm")], phases.Select(stats => (stats.Model, stats.Phase)));
        var warm = phases.Single(stats => stats is { Model: "m1", Phase: "warm" });
        Assert.Equal((2.0, 5.0, 150.0, 29.0, 2), (warm.FirstTokenSeconds, warm.TotalSeconds, warm.CompletionTokens, warm.TokensPerSecond, warm.Requests));
    }

    /// <summary>Boundary: a figure that no request measured stays null and is not turned into zero.</summary>
    [Fact]
    public void Phases_NoRequestMeasuredTheFirstToken_KeepsItNull()
    {
        // Arrange
        var results = new[] { Hotel("m1", cold: true, first: null, total: 2, tokens: 10, speed: null) };

        // Act
        var stats = Assert.Single(BenchmarkResultAnalysis.Phases(results));

        // Assert
        Assert.Null(stats.FirstTokenSeconds);
        Assert.Null(stats.TokensPerSecond);
    }

    /// <summary>Boundary: no request gives no rows.</summary>
    [Fact]
    public void Phases_NoRequests_ReturnsNoRows() => Assert.Empty(BenchmarkResultAnalysis.Phases([]));

    #endregion

    #region Outcomes

    /// <summary>Positive: the outcome counts the passed runs of the graded ones and ignores the hotel analysis and failures.</summary>
    [Fact]
    public void TaskOutcomes_RunsOfATask_CountsPassedOfGraded()
    {
        // Arrange
        var results = new[]
        {
            Task("m1", "math-1", true),
            Task("m1", "math-1", false),
            Task("m1", "code-1", true),
            Task("m1", "extract-1", true, hasError: true),
            Hotel("m1"),
        };

        // Act
        var outcomes = BenchmarkResultAnalysis.TaskOutcomes(results);

        // Assert
        Assert.Equal([("code-1", 1, 1, true), ("math-1", 1, 2, false)], outcomes.Select(outcome => (outcome.TaskId, outcome.Passed, outcome.Total, outcome.AllPassed)));
    }

    /// <summary>Boundary: a task that was not graded has no outcome.</summary>
    [Fact]
    public void TaskOutcomes_UngradedTask_HasNoOutcome() =>
        Assert.Empty(BenchmarkResultAnalysis.TaskOutcomes([Task("m1", "summ-1", null)]));

    #endregion

    #region Resources

    /// <summary>Positive: the memory is the mean per model of the figures that were measured.</summary>
    [Fact]
    public void Resources_TwoRequests_AveragesTheMeasuredFigures()
    {
        // Arrange
        var results = new[]
        {
            Hotel("m1", ram: (1000, 1100, 2000), vram: (null, null, null)),
            Hotel("m1", ram: (3000, 3100, 4000), vram: (null, null, null)),
        };

        // Act
        var usage = Assert.Single(BenchmarkResultAnalysis.Resources(results));

        // Assert
        Assert.Equal((2000.0, 2100.0, 3000.0), (usage.RamBeforeMb, usage.RamAfterMb, usage.RamPeakMb));
        Assert.Null(usage.VramPeakMb);
    }

    #endregion

    #region Helpers

    private static BenchmarkResultDto Hotel(
        string model,
        bool cold = true,
        double? first = 1,
        double total = 2,
        int tokens = 10,
        double? speed = 5,
        bool hasError = false,
        (double? Before, double? After, double? Peak)? ram = null,
        (double? Before, double? After, double? Peak)? vram = null) =>
        Result(model, "hotel_cancellation", "hotel-cancellation", cold, first, total, tokens, speed, null, hasError, ram, vram);

    private static BenchmarkResultDto Task(string model, string task, bool? passed, bool hasError = false) =>
        Result(model, "deterministic_task", task, false, 1, 2, 10, 5, passed, hasError, null, null);

    private static BenchmarkResultDto Result(
        string model,
        string analysisType,
        string task,
        bool cold,
        double? first,
        double total,
        int tokens,
        double? speed,
        bool? passed,
        bool hasError,
        (double? Before, double? After, double? Peak)? ram,
        (double? Before, double? After, double? Peak)? vram) =>
        new(
            ++_next,
            At,
            model,
            analysisType,
            task,
            0,
            cold,
            first,
            total,
            tokens,
            speed,
            false,
            ram?.Peak,
            vram?.Peak,
            passed,
            null,
            hasError,
            ram?.Before,
            ram?.After,
            vram?.Before,
            vram?.After,
            "stop");

    #endregion
}
