using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Benchmark;
using NFHotel.Web.Common;
using NFHotel.Web.Features.Admin.Benchmark;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>The list of stored benchmark runs, one run opened, and its answer read on its own (ADR-0013).</summary>
public sealed class BenchmarkRunListViewModelTests
{
    private static readonly Guid RunId = Guid.Parse("5b3f0e1a-0000-4000-8000-000000000001");
    private static readonly DateTimeOffset Start = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);

    #region The list

    /// <summary>Positive: the runs are loaded and shown.</summary>
    [Fact]
    public async Task InitializeAsync_RunsExist_LoadsThem()
    {
        // Arrange
        var viewModel = Build(out _);

        // Act
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(viewModel.IsLoaded);
        Assert.Equal(RunId, Assert.Single(viewModel.Runs).RunId);
        Assert.Empty(viewModel.Errors);
        Assert.False(viewModel.IsEmpty);
    }

    /// <summary>Boundary: no run yet is an empty list and not an error.</summary>
    [Fact]
    public async Task InitializeAsync_NoRuns_IsEmptyWithoutErrors()
    {
        // Arrange
        var viewModel = Build(out var service);
        service.GetRunsAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<BenchmarkRunListItemDto>>.Success([]));

        // Act
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(viewModel.IsEmpty);
        Assert.Empty(viewModel.Errors);
    }

    /// <summary>Negative: a failing list shows the text of the code and no runs.</summary>
    [Fact]
    public async Task InitializeAsync_ApiUnavailable_ShowsTheTextAndNoRuns()
    {
        // Arrange
        var viewModel = Build(out var service);
        service.GetRunsAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<BenchmarkRunListItemDto>>.Failure(ErrorCodes.Api.Unavailable));

        // Act
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(viewModel.Runs);
        Assert.Equal("We couldn't reach the hotel system. Please try again in a moment.", Assert.Single(viewModel.Errors));
        Assert.True(viewModel.IsLoaded);
        Assert.False(viewModel.IsEmpty);
    }

    #endregion

    #region One run

    /// <summary>Positive: opening a run loads its requests and clears an answer that was shown before.</summary>
    [Fact]
    public async Task SelectAsync_Run_LoadsTheDetailsAndClearsTheShownAnswer()
    {
        // Arrange
        var viewModel = Build(out _);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        await viewModel.ShowOutputAsync(7, TestContext.Current.CancellationToken);

        // Act
        await viewModel.SelectAsync(RunId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(RunId, viewModel.SelectedRun!.Header.RunId);
        Assert.Null(viewModel.ShownOutput);
    }

    /// <summary>Negative: a run that has vanished shows the not found text and nothing is selected.</summary>
    [Fact]
    public async Task SelectAsync_UnknownRun_ShowsTheNotFoundTextAndSelectsNothing()
    {
        // Arrange
        var viewModel = Build(out var service);
        service.GetRunAsync(RunId, Arg.Any<CancellationToken>()).Returns(Result<BenchmarkRunDetailsDto>.Failure(ErrorCodes.Benchmark.NotFound));

        // Act
        await viewModel.SelectAsync(RunId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(viewModel.SelectedRun);
        Assert.Equal("The benchmark run was not found.", Assert.Single(viewModel.Errors));
    }

    /// <summary>State: selecting the run that is open closes it.</summary>
    [Fact]
    public async Task SelectAsync_SameRunTwice_ClosesTheRun()
    {
        // Arrange
        var viewModel = Build(out _);
        await viewModel.SelectAsync(RunId, TestContext.Current.CancellationToken);

        // Act
        await viewModel.SelectAsync(RunId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(viewModel.SelectedRun);
    }

    #endregion

    #region The answer

    /// <summary>Positive: the answer of one request is read on its own and hidden again.</summary>
    [Fact]
    public async Task ShowOutputAsync_Request_ReadsTheAnswerAndHideOutputClearsIt()
    {
        // Arrange
        var viewModel = Build(out _);

        // Act
        await viewModel.ShowOutputAsync(7, TestContext.Current.CancellationToken);
        var shown = viewModel.ShownOutput;
        viewModel.HideOutput();

        // Assert
        Assert.Equal("{}", shown!.Output);
        Assert.Null(viewModel.ShownOutput);
    }

    /// <summary>Negative: an answer that cannot be read shows the text of the code.</summary>
    [Fact]
    public async Task ShowOutputAsync_Unknown_ShowsTheNotFoundText()
    {
        // Arrange
        var viewModel = Build(out var service);
        service.GetOutputAsync(8, Arg.Any<CancellationToken>()).Returns(Result<BenchmarkOutputDto>.Failure(ErrorCodes.Benchmark.NotFound));

        // Act
        await viewModel.ShowOutputAsync(8, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(viewModel.ShownOutput);
        Assert.Equal("The benchmark run was not found.", Assert.Single(viewModel.Errors));
    }

    #endregion

    #region Helpers

    private static BenchmarkRunListViewModel Build(out IBenchmarkService service)
    {
        var header = new BenchmarkRunListItemDto(RunId, Start, Start.AddMinutes(5), "d.csv", "Test CPU", 16384, null, "ollama", "0.5.1", ["m1"], 1);
        var details = new BenchmarkRunDetailsDto(header, new string('a', 64), 10, "hotel-v1", 8, "Linux", []);
        var fake = Substitute.For<IBenchmarkService>();
        fake.GetRunsAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<BenchmarkRunListItemDto>>.Success([header]));
        fake.GetRunAsync(RunId, Arg.Any<CancellationToken>()).Returns(Result<BenchmarkRunDetailsDto>.Success(details));
        fake.GetOutputAsync(7, Arg.Any<CancellationToken>()).Returns(Result<BenchmarkOutputDto>.Success(new BenchmarkOutputDto(7, "m1", "{}", null, null)));
        service = fake;

        var runner = Substitute.For<IUseCaseRunner<IBenchmarkService>>();
        runner.RunAsync(
                Arg.Any<Func<IBenchmarkService, CancellationToken, Task<Result<IReadOnlyList<BenchmarkRunListItemDto>>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<IBenchmarkService, CancellationToken, Task<Result<IReadOnlyList<BenchmarkRunListItemDto>>>>>()(fake, call.Arg<CancellationToken>()));
        runner.RunAsync(
                Arg.Any<Func<IBenchmarkService, CancellationToken, Task<Result<BenchmarkRunDetailsDto>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<IBenchmarkService, CancellationToken, Task<Result<BenchmarkRunDetailsDto>>>>()(fake, call.Arg<CancellationToken>()));
        runner.RunAsync(
                Arg.Any<Func<IBenchmarkService, CancellationToken, Task<Result<BenchmarkOutputDto>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<IBenchmarkService, CancellationToken, Task<Result<BenchmarkOutputDto>>>>()(fake, call.Arg<CancellationToken>()));
        return new BenchmarkRunListViewModel(runner);
    }

    #endregion
}
