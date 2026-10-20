using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Benchmark;
using NFHotel.Web.Common;
using NFHotel.Web.Features.Admin.Benchmark;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>The state of the analysis view: the workload and the answers that are compared side by side.</summary>
public sealed class BenchmarkAnalysisViewModelTests
{
    private static readonly Guid RunId = Guid.Parse("5b3f0e1a-0000-4000-8000-000000000001");

    /// <summary>Positive: choosing an answer reads it once and adds it to the comparison.</summary>
    [Fact]
    public async Task ToggleCompareAsync_NewAnswer_ReadsItAndAddsIt()
    {
        // Arrange
        var viewModel = Build(out var service);
        viewModel.ShowRun(RunId);

        // Act
        await viewModel.ToggleCompareAsync(1, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([1], viewModel.Compared);
        Assert.Equal("answer 1", viewModel.Outputs[1].Output);
        await service.Received(1).GetOutputAsync(1, Arg.Any<CancellationToken>());
    }

    /// <summary>State: choosing an answer again takes it out, and choosing it later does not read it again.</summary>
    [Fact]
    public async Task ToggleCompareAsync_ChosenTwice_TakesItOutAndDoesNotReadItAgain()
    {
        // Arrange
        var viewModel = Build(out var service);
        viewModel.ShowRun(RunId);
        await viewModel.ToggleCompareAsync(1, TestContext.Current.CancellationToken);

        // Act
        await viewModel.ToggleCompareAsync(1, TestContext.Current.CancellationToken);
        var emptyAfterTakingOut = viewModel.Compared.Count;
        await viewModel.ToggleCompareAsync(1, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, emptyAfterTakingOut);
        Assert.Equal([1], viewModel.Compared);
        await service.Received(1).GetOutputAsync(1, Arg.Any<CancellationToken>());
    }

    /// <summary>Boundary: at most three answers are compared, and the oldest choice leaves first.</summary>
    [Fact]
    public async Task ToggleCompareAsync_FourAnswers_KeepsTheNewestThree()
    {
        // Arrange
        var viewModel = Build(out _);
        viewModel.ShowRun(RunId);

        // Act
        foreach (var id in new[] { 1, 2, 3, 4 })
        {
            await viewModel.ToggleCompareAsync(id, TestContext.Current.CancellationToken);
        }

        // Assert
        Assert.Equal([2, 3, 4], viewModel.Compared);
        Assert.False(viewModel.IsCompared(1));
    }

    /// <summary>Negative: an answer that cannot be read shows the text of the code and is not compared.</summary>
    [Fact]
    public async Task ToggleCompareAsync_NotFound_ShowsTheTextAndAddsNothing()
    {
        // Arrange
        var viewModel = Build(out var service);
        viewModel.ShowRun(RunId);
        service.GetOutputAsync(9, Arg.Any<CancellationToken>()).Returns(Result<BenchmarkOutputDto>.Failure(ErrorCodes.Benchmark.NotFound));

        // Act
        await viewModel.ToggleCompareAsync(9, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("The benchmark run was not found.", Assert.Single(viewModel.Errors));
        Assert.Empty(viewModel.Compared);
    }

    /// <summary>State: showing another run forgets the answers of the first, and showing the same run keeps them.</summary>
    [Fact]
    public async Task ShowRun_AnotherRun_ForgetsTheComparison()
    {
        // Arrange
        var viewModel = Build(out _);
        viewModel.ShowRun(RunId);
        await viewModel.ToggleCompareAsync(1, TestContext.Current.CancellationToken);

        // Act
        viewModel.ShowRun(RunId);
        var keptForTheSameRun = viewModel.Compared.Count;
        viewModel.ShowRun(Guid.NewGuid());

        // Assert
        Assert.Equal(1, keptForTheSameRun);
        Assert.Empty(viewModel.Compared);
        Assert.Empty(viewModel.Outputs);
    }

    /// <summary>State: the workload starts as the hotel analysis, the one the notebook starts with.</summary>
    [Fact]
    public void Workload_Default_IsTheHotelAnalysis() =>
        Assert.Equal(BenchmarkWorkload.Hotel, Build(out _).Workload);

    private static BenchmarkAnalysisViewModel Build(out IBenchmarkService service)
    {
        var fake = Substitute.For<IBenchmarkService>();
        fake.GetOutputAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<BenchmarkOutputDto>.Success(new BenchmarkOutputDto(call.ArgAt<int>(0), "m1", $"answer {call.ArgAt<int>(0)}", null, null)));
        service = fake;

        var runner = Substitute.For<IUseCaseRunner<IBenchmarkService>>();
        runner.RunAsync(
                Arg.Any<Func<IBenchmarkService, CancellationToken, Task<Result<BenchmarkOutputDto>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<IBenchmarkService, CancellationToken, Task<Result<BenchmarkOutputDto>>>>()(fake, call.Arg<CancellationToken>()));

        return new BenchmarkAnalysisViewModel(runner);
    }
}
