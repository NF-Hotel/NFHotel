using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Benchmark;

/// <summary>The rules of a stored benchmark run and its requests (ADR-0008).</summary>
public sealed class BenchmarkRunTests
{
    private static readonly Guid RunId = Guid.Parse("5b3f0e1a-0000-4000-8000-000000000001");
    private static readonly DateTimeOffset Start = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);
    private static readonly string Hash = new('a', 64);
    private static readonly BenchmarkMachine Machine = new("Test CPU", 8, 16384, null, "Linux 6.8");

    #region Creation

    /// <summary>Positive: a valid run keeps its header, its machine and every request in order.</summary>
    [Fact]
    public void Create_ValidFields_KeepsHeaderMachineAndResultsInOrder()
    {
        // Arrange and act
        var run = Create(results: [Result("m1"), Result("m2")]);

        // Assert
        Assert.Equal(RunId, run.RunId);
        Assert.Equal("d.csv", run.DatasetName);
        Assert.Equal(Hash, run.DatasetHash);
        Assert.Equal("Test CPU", run.Cpu);
        Assert.Null(run.Gpu);
        Assert.Equal(["m1", "m2"], run.Results.Select(result => result.Model));
    }

    /// <summary>Positive: the measurements of a request that could not be measured stay null and are not turned into zero.</summary>
    [Fact]
    public void Create_RequestWithoutMemoryFigures_KeepsThemNull()
    {
        // Arrange and act
        var result = Create(results: [Result("m1")]).Results[0];

        // Assert
        Assert.Null(result.RamPeakMb);
        Assert.Null(result.VramPeakMb);
        Assert.Null(result.TimeToFirstTokenSeconds);
    }

    /// <summary>Equivalence: both known analysis types are accepted.</summary>
    [Theory]
    [InlineData(BenchmarkResult.HotelCancellation)]
    [InlineData(BenchmarkResult.DeterministicTask)]
    public void Create_KnownAnalysisType_Accepts(string analysisType)
    {
        // Arrange and act
        var run = Create(results: [Result("m1") with { AnalysisType = analysisType }]);

        // Assert
        Assert.Equal(analysisType, run.Results[0].AnalysisType);
    }

    #endregion

    #region Invalid header

    /// <summary>Negative: the empty id is refused with the run id code.</summary>
    [Fact]
    public void Create_EmptyRunId_ThrowsTheRunIdCode()
    {
        // Arrange and act
        var error = Assert.Throws<DomainException>(() => Create(runId: Guid.Empty));

        // Assert
        Assert.Contains(BenchmarkErrors.RunIdRequired, error.Codes);
    }

    /// <summary>Boundary: a run that ends at the moment it started is accepted, one that ends before is refused.</summary>
    [Fact]
    public void Create_EndAtAndBeforeStart_AcceptsTheSameInstantAndRefusesEarlier()
    {
        // Arrange and act
        var same = Create(finishedAt: Start);
        var error = Assert.Throws<DomainException>(() => Create(finishedAt: Start.AddSeconds(-1)));

        // Assert
        Assert.Equal(Start, same.FinishedAt);
        Assert.Contains(BenchmarkErrors.EndBeforeStart, error.Codes);
    }

    /// <summary>Negative: a dataset hash that is not 64 lower-case hexadecimal characters is refused.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("A0000000000000000000000000000000000000000000000000000000000000000")]
    public void Create_DatasetHashNotSha256_ThrowsTheHashCode(string hash)
    {
        // Arrange and act
        var error = Assert.Throws<DomainException>(() => Create(datasetHash: hash));

        // Assert
        Assert.Contains(BenchmarkErrors.DatasetHashInvalid, error.Codes);
    }

    /// <summary>Negative: a blank dataset name, prompt version or backend is refused with its own code.</summary>
    [Fact]
    public void Create_BlankNamesInTheHeader_ThrowsOneCodeForEach()
    {
        // Arrange and act
        var error = Assert.Throws<DomainException>(() =>
            BenchmarkRun.Create(RunId, Start, Start, string.Empty, Hash, 10, string.Empty, Machine, string.Empty, null, [Result("m1")]));

        // Assert
        Assert.Contains(BenchmarkErrors.DatasetNameInvalid, error.Codes);
        Assert.Contains(BenchmarkErrors.PromptVersionInvalid, error.Codes);
        Assert.Contains(BenchmarkErrors.BackendInvalid, error.Codes);
    }

    /// <summary>Boundary: zero rows are accepted, a negative number of rows is refused.</summary>
    [Fact]
    public void Create_DatasetRowsAtAndBelowZero_AcceptsZeroAndRefusesNegative()
    {
        // Arrange and act
        var zero = Create(datasetRows: 0);
        var error = Assert.Throws<DomainException>(() => Create(datasetRows: -1));

        // Assert
        Assert.Equal(0, zero.DatasetRows);
        Assert.Contains(BenchmarkErrors.DatasetRowsInvalid, error.Codes);
    }

    /// <summary>Negative: a machine without cores, memory or CPU name is refused with the machine code.</summary>
    [Theory]
    [InlineData("", 8, 16384)]
    [InlineData("Test CPU", 0, 16384)]
    [InlineData("Test CPU", 8, 0)]
    public void Create_IncompleteMachine_ThrowsTheMachineCode(string cpu, int cores, int memoryMb)
    {
        // Arrange and act
        var error = Assert.Throws<DomainException>(() => Create(machine: new BenchmarkMachine(cpu, cores, memoryMb, null, "Linux")));

        // Assert
        Assert.Contains(BenchmarkErrors.MachineInvalid, error.Codes);
    }

    #endregion

    #region Invalid requests

    /// <summary>Negative: a run without any request is refused.</summary>
    [Fact]
    public void Create_NoResults_ThrowsTheResultsCode()
    {
        // Arrange and act
        var error = Assert.Throws<DomainException>(() => Create(results: []));

        // Assert
        Assert.Contains(BenchmarkErrors.ResultsRequired, error.Codes);
    }

    /// <summary>Negative: an unknown analysis type is refused.</summary>
    [Fact]
    public void Create_UnknownAnalysisType_ThrowsTheAnalysisCode()
    {
        // Arrange and act
        var error = Assert.Throws<DomainException>(() => Create(results: [Result("m1") with { AnalysisType = "other" }]));

        // Assert
        Assert.Contains(BenchmarkErrors.AnalysisTypeInvalid, error.Codes);
    }

    /// <summary>Negative: a blank model name is refused.</summary>
    [Fact]
    public void Create_BlankModel_ThrowsTheModelCode()
    {
        // Arrange and act
        var error = Assert.Throws<DomainException>(() => Create(results: [Result(string.Empty)]));

        // Assert
        Assert.Contains(BenchmarkErrors.ModelInvalid, error.Codes);
    }

    /// <summary>Boundary: zero seconds and zero tokens are accepted, a negative figure is refused.</summary>
    [Fact]
    public void Create_MeasurementsAtAndBelowZero_AcceptsZeroAndRefusesNegative()
    {
        // Arrange and act
        var zero = Create(results: [Result("m1") with { TotalSeconds = 0, CompletionTokens = 0, RamPeakMb = 0 }]);
        var error = Assert.Throws<DomainException>(() => Create(results: [Result("m1") with { TotalSeconds = -0.1 }]));
        var tokens = Assert.Throws<DomainException>(() => Create(results: [Result("m1") with { CompletionTokens = -1 }]));
        var memory = Assert.Throws<DomainException>(() => Create(results: [Result("m1") with { VramPeakMb = -1 }]));

        // Assert
        Assert.Equal(0, zero.Results[0].TotalSeconds);
        Assert.Contains(BenchmarkErrors.MeasurementInvalid, error.Codes);
        Assert.Contains(BenchmarkErrors.MeasurementInvalid, tokens.Codes);
        Assert.Contains(BenchmarkErrors.MeasurementInvalid, memory.Codes);
    }

    /// <summary>Negative: a negative repetition number is refused.</summary>
    [Fact]
    public void Create_NegativeRunNumber_ThrowsTheRunNumberCode()
    {
        // Arrange and act
        var error = Assert.Throws<DomainException>(() => Create(results: [Result("m1") with { RunNumber = -1 }]));

        // Assert
        Assert.Contains(BenchmarkErrors.RunNumberInvalid, error.Codes);
    }

    /// <summary>State: several faults in one delivery are all reported once, so the client can fix them together.</summary>
    [Fact]
    public void Create_SeveralFaults_ReportsEachCodeOnce()
    {
        // Arrange and act
        var error = Assert.Throws<DomainException>(() => Create(
            runId: Guid.Empty,
            results: [Result(string.Empty), Result(string.Empty)]));

        // Assert
        Assert.Equal(error.Codes.Count, error.Codes.Distinct().Count());
        Assert.Contains(BenchmarkErrors.RunIdRequired, error.Codes);
        Assert.Contains(BenchmarkErrors.ModelInvalid, error.Codes);
    }

    #endregion

    #region Helpers

    private static BenchmarkRun Create(
        Guid? runId = null,
        DateTimeOffset? finishedAt = null,
        string? datasetHash = null,
        int datasetRows = 10,
        BenchmarkMachine? machine = null,
        IReadOnlyList<BenchmarkResultData>? results = null) =>
        BenchmarkRun.Create(
            runId ?? RunId,
            Start,
            finishedAt ?? Start.AddMinutes(5),
            "d.csv",
            datasetHash ?? Hash,
            datasetRows,
            "hotel-v1",
            machine ?? Machine,
            "ollama",
            "0.5.1",
            results ?? [Result("m1")]);

    private static BenchmarkResultData Result(string model) =>
        new(
            Start,
            "ollama",
            model,
            BenchmarkResult.HotelCancellation,
            "hotel-cancellation",
            "analysis",
            0,
            true,
            null,
            2.0,
            10,
            20,
            5.0,
            false,
            "stop",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            true,
            null,
            "{}",
            null);

    #endregion
}
