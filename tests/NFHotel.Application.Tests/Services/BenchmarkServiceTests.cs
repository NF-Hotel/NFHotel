using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Errors;
using NFHotel.Domain.Errors;
using NFHotel.TestSupport;

using NSubstitute;

namespace NFHotel.Application.Tests.Services;

/// <summary>Storing and reading the benchmark runs that the benchmark service delivers (ADR-0008).</summary>
public sealed class BenchmarkServiceTests
{
    private static readonly Guid RunId = Guid.Parse("5b3f0e1a-0000-4000-8000-000000000001");
    private static readonly DateTimeOffset Start = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);

    #region Storing a run

    /// <summary>Positive: a new run is stored once with all its requests and the answer is Created.</summary>
    [Fact]
    public async Task StoreAsync_NewRun_StoresItOnceAndReturnsCreated()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await world.BenchmarkService().StoreAsync(Request(), TestContext.Current.CancellationToken);

        // Assert
        var stored = Assert.Single(world.Benchmarks.Items);
        Assert.True(result.IsSuccess);
        Assert.Equal(BenchmarkStoreOutcome.Created, result.Value);
        Assert.Equal(RunId, stored.RunId);
        Assert.Equal(2, stored.Results.Count);
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>Positive: every field of the contract reaches the entity under its own name.</summary>
    [Fact]
    public async Task StoreAsync_NewRun_MapsEveryFieldOfTheContract()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        await world.BenchmarkService().StoreAsync(Request(), TestContext.Current.CancellationToken);

        // Assert
        var run = Assert.Single(world.Benchmarks.Items);
        var first = run.Results[0];
        Assert.Equal("d.csv", run.DatasetName);
        Assert.Equal(new string('a', 64), run.DatasetHash);
        Assert.Equal(10, run.DatasetRows);
        Assert.Equal("hotel-v1", run.PromptVersion);
        Assert.Equal(("Test CPU", 8, 16384, "Linux 6.8"), (run.Cpu, run.CpuCores, run.MemoryMb, run.OperatingSystem));
        Assert.Null(run.Gpu);
        Assert.Equal(("ollama", "0.5.1"), (run.BackendName, run.BackendVersion));
        Assert.Equal("m1", first.Model);
        Assert.Equal(0.5, first.TimeToFirstTokenSeconds);
        Assert.Equal(2.0, first.TotalSeconds);
        Assert.Equal(10, first.CompletionTokens);
        Assert.Equal(20, first.PromptTokens);
        Assert.Equal(5.0, first.TokensPerSecond);
        Assert.Equal(1000, first.RamPeakMb);
        Assert.Equal(2000, first.VramPeakMb);
        Assert.True(first.Cold);
        Assert.Equal("{}", first.Output);
    }

    /// <summary>Positive: a repeat of a stored run id answers AlreadyStored and stores nothing new.</summary>
    [Fact]
    public async Task StoreAsync_SameRunIdTwice_ReturnsAlreadyStoredAndStoresNothingNew()
    {
        // Arrange
        var world = new ServiceWorld();
        var service = world.BenchmarkService();
        await service.StoreAsync(Request(), TestContext.Current.CancellationToken);

        // Act
        var second = await service.StoreAsync(Request(), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(second.IsSuccess);
        Assert.Equal(BenchmarkStoreOutcome.AlreadyStored, second.Value);
        Assert.Single(world.Benchmarks.Items);
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>Equivalence: a repeat is answered AlreadyStored whatever its body says, even an invalid one.</summary>
    [Fact]
    public async Task StoreAsync_RepeatWithAnInvalidBody_StillReturnsAlreadyStored()
    {
        // Arrange
        var world = new ServiceWorld();
        var service = world.BenchmarkService();
        await service.StoreAsync(Request(), TestContext.Current.CancellationToken);
        var broken = Request() with { SchemaVersion = 9, Results = [] };

        // Act
        var result = await service.StoreAsync(broken, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(BenchmarkStoreOutcome.AlreadyStored, result.Value);
    }

    /// <summary>Race: two deliveries at the same moment, the loser hits the unique index and also gets AlreadyStored.</summary>
    [Fact]
    public async Task StoreAsync_UniqueIndexRefusesTheSave_ReturnsAlreadyStored()
    {
        // Arrange
        var world = new ServiceWorld();
        world.UnitOfWork.FailWith = new UniqueConstraintViolationException("duplicate", "ux_benchmark_run_run_id");

        // Act
        var result = await world.BenchmarkService().StoreAsync(Request(), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(BenchmarkStoreOutcome.AlreadyStored, result.Value);
    }

    #endregion

    #region Refusals

    /// <summary>Negative: a schema version other than 2 is refused with its code and nothing is stored.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public async Task StoreAsync_SchemaVersionOtherThanTwo_FailsWithTheUnsupportedCode(int version)
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await world.BenchmarkService().StoreAsync(Request() with { SchemaVersion = version }, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Benchmark.SchemaVersionUnsupported, Assert.Single(result.Errors).Code);
        Assert.Empty(world.Benchmarks.Items);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: a field that breaks a domain rule is refused with the domain code and nothing is stored.</summary>
    [Fact]
    public async Task StoreAsync_InvalidFields_FailsWithTheDomainCodesAndStoresNothing()
    {
        // Arrange
        var world = new ServiceWorld();
        var request = Request() with { Dataset = new BenchmarkDatasetDto("d.csv", "nothex", 10), Results = [] };

        // Act
        var result = await world.BenchmarkService().StoreAsync(request, TestContext.Current.CancellationToken);

        // Assert
        var codes = result.Errors.Select(error => error.Code).ToArray();
        Assert.False(result.IsSuccess);
        Assert.Contains(BenchmarkErrors.DatasetHashInvalid, codes);
        Assert.Contains(BenchmarkErrors.ResultsRequired, codes);
        Assert.Empty(world.Benchmarks.Items);
    }

    /// <summary>Negative: a body without the dataset, the machine, the backend or the results list is refused with the body code.</summary>
    [Fact]
    public async Task StoreAsync_PartsOfTheBodyAreMissing_FailsWithTheBodyCode()
    {
        // Arrange
        var world = new ServiceWorld();
        var service = world.BenchmarkService();

        // Act
        var noDataset = await service.StoreAsync(Request() with { Dataset = null! }, TestContext.Current.CancellationToken);
        var noMachine = await service.StoreAsync(Request() with { Machine = null! }, TestContext.Current.CancellationToken);
        var noBackend = await service.StoreAsync(Request() with { Backend = null! }, TestContext.Current.CancellationToken);
        var noResults = await service.StoreAsync(Request() with { Results = null! }, TestContext.Current.CancellationToken);

        // Assert
        foreach (var result in new[] { noDataset, noMachine, noBackend, noResults })
        {
            Assert.Equal(ErrorCodes.Benchmark.BodyInvalid, Assert.Single(result.Errors).Code);
        }

        Assert.Empty(world.Benchmarks.Items);
    }

    /// <summary>Negative: a missing request is a programming error and throws.</summary>
    [Fact]
    public async Task StoreAsync_NullRequest_Throws()
    {
        // Arrange
        var service = new ServiceWorld().BenchmarkService();

        // Act and assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.StoreAsync(null!, TestContext.Current.CancellationToken));
    }

    #endregion

    #region Reading

    /// <summary>Positive: the list is what the read port returns.</summary>
    [Fact]
    public async Task GetRunsAsync_RunsExist_ReturnsThemFromTheReadPort()
    {
        // Arrange
        var world = new ServiceWorld();
        var item = new BenchmarkRunListItemDto(RunId, Start, Start.AddMinutes(5), "d.csv", "CPU", 16384, null, "ollama", "0.5.1", ["m1"], 1);
        world.BenchmarkQueries.GetRunsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<BenchmarkRunListItemDto>>([item]));

        // Act
        var result = await world.BenchmarkService().GetRunsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(item, Assert.Single(result.Value));
    }

    /// <summary>Negative: an unknown run id fails with the not found code.</summary>
    [Fact]
    public async Task GetRunAsync_UnknownRunId_FailsWithNotFound()
    {
        // Arrange
        var world = new ServiceWorld();
        world.BenchmarkQueries.GetRunAsync(RunId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<BenchmarkRunDetailsDto?>(null));

        // Act
        var result = await world.BenchmarkService().GetRunAsync(RunId, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Benchmark.NotFound, Assert.Single(result.Errors).Code);
    }

    /// <summary>Positive: a known run id returns the details.</summary>
    [Fact]
    public async Task GetRunAsync_KnownRunId_ReturnsTheDetails()
    {
        // Arrange
        var world = new ServiceWorld();
        var header = new BenchmarkRunListItemDto(RunId, Start, Start.AddMinutes(5), "d.csv", "CPU", 16384, null, "ollama", "0.5.1", ["m1"], 0);
        var details = new BenchmarkRunDetailsDto(header, new string('a', 64), 10, "hotel-v1", 8, "Linux", []);
        world.BenchmarkQueries.GetRunAsync(RunId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<BenchmarkRunDetailsDto?>(details));

        // Act
        var result = await world.BenchmarkService().GetRunAsync(RunId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(details, result.Value);
    }

    /// <summary>Negative: the empty run id is not looked up and fails with the not found code.</summary>
    [Fact]
    public async Task GetRunAsync_EmptyRunId_FailsWithNotFoundWithoutAskingTheReadPort()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await world.BenchmarkService().GetRunAsync(Guid.Empty, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Benchmark.NotFound, Assert.Single(result.Errors).Code);
        await world.BenchmarkQueries.DidNotReceive().GetRunAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>State: the answer of a request is read on its own and an unknown key fails with not found.</summary>
    [Fact]
    public async Task GetOutputAsync_KnownAndUnknownKey_ReturnsTheAnswerOrNotFound()
    {
        // Arrange
        var world = new ServiceWorld();
        var output = new BenchmarkOutputDto(7, "m1", "{}", null, null);
        world.BenchmarkQueries.GetOutputAsync(7, Arg.Any<CancellationToken>()).Returns(Task.FromResult<BenchmarkOutputDto?>(output));
        world.BenchmarkQueries.GetOutputAsync(8, Arg.Any<CancellationToken>()).Returns(Task.FromResult<BenchmarkOutputDto?>(null));
        var service = world.BenchmarkService();

        // Act
        var known = await service.GetOutputAsync(7, TestContext.Current.CancellationToken);
        var unknown = await service.GetOutputAsync(8, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(output, known.Value);
        Assert.Equal(ErrorCodes.Benchmark.NotFound, Assert.Single(unknown.Errors).Code);
    }

    #endregion

    #region Helpers

    private static SubmitBenchmarkRunRequest Request() =>
        new(
            2,
            RunId,
            Start,
            Start.AddMinutes(5),
            "hotel-v1",
            new BenchmarkDatasetDto("d.csv", new string('a', 64), 10),
            new BenchmarkMachineDto("Test CPU", 8, 16384, null, "Linux 6.8"),
            new BenchmarkBackendDto("ollama", "0.5.1"),
            [Result("m1", cold: true), Result("m2", cold: false)]);

    private static BenchmarkRequestResultDto Result(string model, bool cold) =>
        new(
            2,
            Start,
            "s1",
            "ollama",
            model,
            "hotel-cancellation",
            "analysis",
            "hotel_cancellation",
            0,
            cold,
            0.5,
            2.0,
            10,
            20,
            5.0,
            false,
            "stop",
            900,
            950,
            1000,
            1800,
            1900,
            2000,
            null,
            true,
            null,
            "{}",
            null);

    #endregion
}