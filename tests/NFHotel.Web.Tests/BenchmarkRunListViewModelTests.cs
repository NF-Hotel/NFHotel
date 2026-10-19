using NFHotel.Application.Dto.AiDefaults;
using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.AiDefaults;
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

    #region Deleting a run

    /// <summary>State: asking to delete a run waits for the confirmation and deletes nothing.</summary>
    [Fact]
    public async Task RequestDelete_Run_WaitsForTheConfirmation()
    {
        // Arrange
        var viewModel = Build(out var service);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);

        // Act
        viewModel.RequestDelete(viewModel.Runs[0]);

        // Assert
        Assert.Equal(RunId, viewModel.RunPendingDeletion!.RunId);
        await service.DidNotReceive().DeleteRunAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>State: backing out clears the question and deletes nothing.</summary>
    [Fact]
    public async Task AbortDelete_AfterTheRequest_ClearsItWithoutDeleting()
    {
        // Arrange
        var viewModel = Build(out var service);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        viewModel.RequestDelete(viewModel.Runs[0]);

        // Act
        viewModel.AbortDelete();

        // Assert
        Assert.Null(viewModel.RunPendingDeletion);
        await service.DidNotReceive().DeleteRunAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Positive: confirming deletes the run, closes it when it is open and reads the list again.</summary>
    [Fact]
    public async Task ConfirmDeleteAsync_OpenRun_DeletesItClosesItAndReadsTheListAgain()
    {
        // Arrange
        var viewModel = Build(out var service);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        await viewModel.SelectAsync(RunId, TestContext.Current.CancellationToken);
        viewModel.RequestDelete(viewModel.Runs[0]);
        service.GetRunsAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<BenchmarkRunListItemDto>>.Success([]));

        // Act
        await viewModel.ConfirmDeleteAsync(TestContext.Current.CancellationToken);

        // Assert
        await service.Received(1).DeleteRunAsync(RunId, Arg.Any<CancellationToken>());
        Assert.Null(viewModel.RunPendingDeletion);
        Assert.Null(viewModel.SelectedRun);
        Assert.True(viewModel.IsEmpty);
        Assert.Empty(viewModel.Errors);
    }

    /// <summary>Negative: a refused deletion shows the text of the code and keeps the list as it was.</summary>
    [Fact]
    public async Task ConfirmDeleteAsync_NotFound_ShowsTheTextAndKeepsTheList()
    {
        // Arrange
        var viewModel = Build(out var service);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        viewModel.RequestDelete(viewModel.Runs[0]);
        service.DeleteRunAsync(RunId, Arg.Any<CancellationToken>()).Returns(Result.Failure(ErrorCodes.Benchmark.NotFound));

        // Act
        await viewModel.ConfirmDeleteAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorMessages.Describe(Result.Failure(ErrorCodes.Benchmark.NotFound)), viewModel.Errors);
        Assert.Null(viewModel.RunPendingDeletion);
        Assert.Single(viewModel.Runs);
    }

    /// <summary>Boundary: confirming with nothing waiting does nothing.</summary>
    [Fact]
    public async Task ConfirmDeleteAsync_NothingPending_DeletesNothing()
    {
        // Arrange
        var viewModel = Build(out var service);

        // Act
        await viewModel.ConfirmDeleteAsync(TestContext.Current.CancellationToken);

        // Assert
        await service.DidNotReceive().DeleteRunAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Default AI models

    /// <summary>Positive: the models seen in the runs are what the Manager can choose from, once each and in order.</summary>
    [Fact]
    public async Task KnownModels_AfterTheRunsAreLoaded_ListsEachModelOnceInOrder()
    {
        // Arrange
        var viewModel = Build(out _);

        // Act
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["m1"], viewModel.KnownModels);
    }

    /// <summary>State: the field shows the saved default until the user types, then the typed text.</summary>
    [Fact]
    public async Task DraftOf_SavedDefaultAndTyping_ShowsTheSavedModelThenTheTypedOne()
    {
        // Arrange
        var viewModel = Build(out _, out var defaults);
        defaults.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<AiDefaultDto>>.Success([new AiDefaultDto("website", "saved", "manager", Start, null)]));
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);

        // Act and assert
        Assert.Equal("saved", viewModel.DraftOf("website"));
        viewModel.SetDraft("website", "typed");
        Assert.Equal("typed", viewModel.DraftOf("website"));
        Assert.Equal(string.Empty, viewModel.DraftOf("market_analysis"));
    }

    /// <summary>Positive: saving the field sets the default with the newest run that has the model, then reads the defaults again.</summary>
    [Fact]
    public async Task SaveDefaultAsync_ModelOfARun_SetsItWithThatRunAndReadsAgain()
    {
        // Arrange
        var viewModel = Build(out _, out var defaults);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        viewModel.SetDraft("market_analysis", " m1 ");

        // Act
        await viewModel.SaveDefaultAsync("market_analysis", TestContext.Current.CancellationToken);

        // Assert
        await defaults.Received(1).SetAsync("market_analysis", Arg.Is<SetAiDefaultRequest>(request => request.Model == "m1" && request.SourceRunId == RunId), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await defaults.Received(2).GetAllAsync(Arg.Any<CancellationToken>());
        Assert.Empty(viewModel.Errors);
    }

    /// <summary>Positive: a typed model that no run has is set without a source run.</summary>
    [Fact]
    public async Task SaveDefaultAsync_ModelNoRunHas_SetsItWithoutASourceRun()
    {
        // Arrange
        var viewModel = Build(out _, out var defaults);
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);
        viewModel.SetDraft("website", "phi3.5");

        // Act
        await viewModel.SaveDefaultAsync("website", TestContext.Current.CancellationToken);

        // Assert
        await defaults.Received(1).SetAsync("website", Arg.Is<SetAiDefaultRequest>(request => request.Model == "phi3.5" && request.SourceRunId == null), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Positive: the button of a result row sets the default to the model and the run of that row.</summary>
    [Fact]
    public async Task SetDefaultAsync_FromAResultRow_SetsTheModelWithTheRun()
    {
        // Arrange
        var viewModel = Build(out _, out var defaults);

        // Act
        await viewModel.SetDefaultAsync("website", "m1", RunId, TestContext.Current.CancellationToken);

        // Assert
        await defaults.Received(1).SetAsync("website", Arg.Is<SetAiDefaultRequest>(request => request.Model == "m1" && request.SourceRunId == RunId), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Negative: a refused default shows the text of its code and reads nothing again.</summary>
    [Fact]
    public async Task SetDefaultAsync_Refused_ShowsTheText()
    {
        // Arrange
        var viewModel = Build(out _, out var defaults);
        defaults.SetAsync(Arg.Any<string>(), Arg.Any<SetAiDefaultRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<AiDefaultDto>.Failure(NFHotel.Domain.Errors.AiDefaultErrors.ModelInvalid));

        // Act
        await viewModel.SetDefaultAsync("website", " ", null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("Enter a model name of at most 100 characters.", Assert.Single(viewModel.Errors));
        await defaults.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Negative: a failing read of the defaults does not hide the runs.</summary>
    [Fact]
    public async Task InitializeAsync_DefaultsUnavailable_StillShowsTheRuns()
    {
        // Arrange
        var viewModel = Build(out _, out var defaults);
        defaults.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<AiDefaultDto>>.Failure(ErrorCodes.Api.Unavailable));

        // Act
        await viewModel.InitializeAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(viewModel.Runs);
        Assert.Empty(viewModel.Defaults);
        Assert.Empty(viewModel.Errors);
    }

    #endregion

    #region Helpers

    private static BenchmarkRunListViewModel Build(out IBenchmarkService service) => Build(out service, out _);

    private static BenchmarkRunListViewModel Build(out IBenchmarkService service, out IAiDefaultService defaults)
    {
        var header = new BenchmarkRunListItemDto(RunId, Start, Start.AddMinutes(5), "d.csv", "Test CPU", 16384, null, "ollama", "0.5.1", ["m1"], 1);
        var details = new BenchmarkRunDetailsDto(header, new string('a', 64), 10, "hotel-v1", 8, "Linux", []);
        var fake = Substitute.For<IBenchmarkService>();
        fake.GetRunsAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<BenchmarkRunListItemDto>>.Success([header]));
        fake.GetRunAsync(RunId, Arg.Any<CancellationToken>()).Returns(Result<BenchmarkRunDetailsDto>.Success(details));
        fake.GetOutputAsync(7, Arg.Any<CancellationToken>()).Returns(Result<BenchmarkOutputDto>.Success(new BenchmarkOutputDto(7, "m1", "{}", null, null)));
        fake.DeleteRunAsync(RunId, Arg.Any<CancellationToken>()).Returns(Result.Success());
        service = fake;
        var fakeDefaults = Substitute.For<IAiDefaultService>();
        fakeDefaults.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<AiDefaultDto>>.Success([]));
        fakeDefaults.SetAsync(Arg.Any<string>(), Arg.Any<SetAiDefaultRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<AiDefaultDto>.Success(new AiDefaultDto(call.ArgAt<string>(0), call.ArgAt<SetAiDefaultRequest>(1).Model, "manager", Start, call.ArgAt<SetAiDefaultRequest>(1).SourceRunId)));
        defaults = fakeDefaults;
        var defaultsRunner = Substitute.For<IUseCaseRunner<IAiDefaultService>>();
        defaultsRunner.RunAsync(
                Arg.Any<Func<IAiDefaultService, CancellationToken, Task<Result<IReadOnlyList<AiDefaultDto>>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<IAiDefaultService, CancellationToken, Task<Result<IReadOnlyList<AiDefaultDto>>>>>()(fakeDefaults, call.Arg<CancellationToken>()));
        defaultsRunner.RunAsync(
                Arg.Any<Func<IAiDefaultService, CancellationToken, Task<Result<AiDefaultDto>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<IAiDefaultService, CancellationToken, Task<Result<AiDefaultDto>>>>()(fakeDefaults, call.Arg<CancellationToken>()));

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
        runner.RunAsync(
                Arg.Any<Func<IBenchmarkService, CancellationToken, Task<Result>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<IBenchmarkService, CancellationToken, Task<Result>>>()(fake, call.Arg<CancellationToken>()));
        return new BenchmarkRunListViewModel(runner, defaultsRunner);
    }

    #endregion
}
