using NFHotel.Application.Dto.AiDefaults;
using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.AiDefaults;
using NFHotel.Web.Common;
using NFHotel.Web.Features.Admin.Benchmark;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>The list of installed models where a Manager adds a model and deletes one (MIL-020, ADR-0005 of the benchmark service).</summary>
public sealed class BenchmarkModelsViewModelTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);

    #region Loading

    /// <summary>Positive: the installed models and the defaults are loaded.</summary>
    [Fact]
    public async Task LoadAsync_ServiceUp_LoadsTheModelsAndTheDefaults()
    {
        // Arrange
        var viewModel = Build(out _, out _);

        // Act
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(viewModel.IsLoaded);
        Assert.Equal(["llama3.2:3b", "phi3.5:latest"], viewModel.Models);
        Assert.Equal("website", Assert.Single(viewModel.Defaults).Purpose);
        Assert.Empty(viewModel.Errors);
    }

    /// <summary>Negative: a service that is down shows the text of the code and no models.</summary>
    [Fact]
    public async Task LoadAsync_ServiceDown_ShowsTheTextAndNoModels()
    {
        // Arrange
        var viewModel = Build(out var client, out _);
        client.ListModelsAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<string>>.Failure(ErrorCodes.BenchmarkRunner.Unavailable));

        // Act
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(viewModel.Models);
        Assert.Single(viewModel.Errors);
        Assert.True(viewModel.IsLoaded);
    }

    /// <summary>Negative: a failing read of the defaults only loses the warning.</summary>
    [Fact]
    public async Task LoadAsync_DefaultsUnavailable_StillShowsTheModels()
    {
        // Arrange
        var viewModel = Build(out _, out var defaults);
        defaults.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<AiDefaultDto>>.Failure(ErrorCodes.Api.Unavailable));

        // Act
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, viewModel.Models.Count);
        Assert.Empty(viewModel.Defaults);
        Assert.Empty(viewModel.Errors);
    }

    #endregion

    #region Adding a model

    /// <summary>State: the Add button needs a text that is shaped like a model name and nothing else going on.</summary>
    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("a b", false)]
    [InlineData("../x", false)]
    [InlineData("phi4-mini", true)]
    [InlineData("  phi4-mini  ", true)]
    [InlineData("hf.co/user/model:Q4_K_M", true)]
    public void CanAdd_Text_FollowsTheShapeOfAModelName(string text, bool expected)
    {
        // Arrange
        var viewModel = Build(out _, out _);

        // Act
        viewModel.NewModelName = text;

        // Assert
        Assert.Equal(expected, viewModel.CanAdd);
    }

    /// <summary>Positive: adding a model starts the download with the trimmed name and follows it.</summary>
    [Fact]
    public async Task StartPullAsync_ValidName_StartsTheDownloadAndFollowsIt()
    {
        // Arrange
        var viewModel = Build(out var client, out _);
        viewModel.NewModelName = "  phi4-mini ";

        // Act
        await viewModel.StartPullAsync(TestContext.Current.CancellationToken);

        // Assert
        await client.Received(1).StartModelPullAsync("phi4-mini", Arg.Any<CancellationToken>());
        Assert.Equal("phi4-mini", viewModel.CurrentPull!.Model);
        Assert.True(viewModel.IsPulling);
        Assert.False(viewModel.CanAdd);
    }

    /// <summary>Negative: an invalid text starts nothing.</summary>
    [Fact]
    public async Task StartPullAsync_InvalidText_CallsNothing()
    {
        // Arrange
        var viewModel = Build(out var client, out _);
        viewModel.NewModelName = "a b";

        // Act
        await viewModel.StartPullAsync(TestContext.Current.CancellationToken);

        // Assert
        await client.DidNotReceive().StartModelPullAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.Null(viewModel.CurrentPull);
    }

    /// <summary>Negative: a refused start shows the text of the code and follows nothing.</summary>
    [Fact]
    public async Task StartPullAsync_AnotherDownloadActive_ShowsTheText()
    {
        // Arrange
        var viewModel = Build(out var client, out _);
        client.StartModelPullAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Result<ModelPullDto>.Failure(ErrorCodes.BenchmarkRunner.PullActive));
        viewModel.NewModelName = "phi4-mini";

        // Act
        await viewModel.StartPullAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("Another model is being added. Wait until it is done.", Assert.Single(viewModel.Errors));
        Assert.Null(viewModel.CurrentPull);
        Assert.False(viewModel.IsPulling);
    }

    /// <summary>State: a download that is done reloads the list, clears the field and stops the following.</summary>
    [Fact]
    public async Task RefreshPullAsync_Done_ReloadsTheModelsAndClearsTheField()
    {
        // Arrange
        var viewModel = Build(out var client, out _);
        viewModel.NewModelName = "phi4-mini";
        await viewModel.StartPullAsync(TestContext.Current.CancellationToken);
        client.GetModelPullAsync("pull-1", Arg.Any<CancellationToken>()).Returns(Result<ModelPullDto>.Success(new ModelPullDto("pull-1", "phi4-mini", ModelPullStates.Done, 100, null)));
        client.ListModelsAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<string>>.Success(["llama3.2:3b", "phi4-mini:latest"]));

        // Act
        await viewModel.RefreshPullAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.False(viewModel.IsPulling);
        Assert.Equal(string.Empty, viewModel.NewModelName);
        Assert.Contains("phi4-mini:latest", viewModel.Models);
    }

    /// <summary>Negative: a failed download keeps the reason and the field, and stops the following.</summary>
    [Fact]
    public async Task RefreshPullAsync_Failed_KeepsTheReasonAndStops()
    {
        // Arrange
        var viewModel = Build(out var client, out _);
        viewModel.NewModelName = "nope";
        await viewModel.StartPullAsync(TestContext.Current.CancellationToken);
        client.GetModelPullAsync("pull-1", Arg.Any<CancellationToken>()).Returns(Result<ModelPullDto>.Success(new ModelPullDto("pull-1", "nope", ModelPullStates.Failed, 40, "file does not exist")));

        // Act
        await viewModel.RefreshPullAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.False(viewModel.IsPulling);
        Assert.Equal("file does not exist", viewModel.CurrentPull!.Error);
        Assert.Equal("nope", viewModel.NewModelName);
    }

    /// <summary>Negative: a download that the service forgot ends the following.</summary>
    [Fact]
    public async Task RefreshPullAsync_ServiceForgotTheDownload_EndsTheFollowing()
    {
        // Arrange
        var viewModel = Build(out var client, out _);
        viewModel.NewModelName = "phi4-mini";
        await viewModel.StartPullAsync(TestContext.Current.CancellationToken);
        client.GetModelPullAsync("pull-1", Arg.Any<CancellationToken>()).Returns(Result<ModelPullDto>.Failure(ErrorCodes.BenchmarkRunner.PullNotFound));

        // Act
        await viewModel.RefreshPullAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(viewModel.CurrentPull);
        Assert.Single(viewModel.Errors);
    }

    #endregion

    #region Deleting a model

    /// <summary>State: asking to delete waits for the confirmation and deletes nothing, and backing out clears it.</summary>
    [Fact]
    public async Task RequestDelete_ThenAbort_DeletesNothing()
    {
        // Arrange
        var viewModel = Build(out var client, out _);

        // Act
        viewModel.RequestDelete("llama3.2:3b");
        var waiting = viewModel.ModelPendingDeletion;
        viewModel.AbortDelete();

        // Assert
        Assert.Equal("llama3.2:3b", waiting);
        Assert.Null(viewModel.ModelPendingDeletion);
        await client.DidNotReceive().DeleteModelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Positive: confirming deletes the model and reads the list again.</summary>
    [Fact]
    public async Task ConfirmDeleteAsync_InstalledModel_DeletesItAndReloads()
    {
        // Arrange
        var viewModel = Build(out var client, out _);
        viewModel.RequestDelete("llama3.2:3b");
        client.ListModelsAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<string>>.Success(["phi3.5:latest"]));

        // Act
        await viewModel.ConfirmDeleteAsync(TestContext.Current.CancellationToken);

        // Assert
        await client.Received(1).DeleteModelAsync("llama3.2:3b", Arg.Any<CancellationToken>());
        Assert.Null(viewModel.ModelPendingDeletion);
        Assert.Equal(["phi3.5:latest"], viewModel.Models);
        Assert.Empty(viewModel.Errors);
    }

    /// <summary>Negative: a model that a run uses stays, with the text of the code.</summary>
    [Fact]
    public async Task ConfirmDeleteAsync_ModelInUse_ShowsTheTextAndKeepsTheList()
    {
        // Arrange
        var viewModel = Build(out var client, out _);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.RequestDelete("llama3.2:3b");
        client.DeleteModelAsync("llama3.2:3b", Arg.Any<CancellationToken>()).Returns(Result.Failure(ErrorCodes.BenchmarkRunner.ModelInUse));

        // Act
        await viewModel.ConfirmDeleteAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("A queued or running benchmark uses this model. Delete it when the run is done.", Assert.Single(viewModel.Errors));
        Assert.Null(viewModel.ModelPendingDeletion);
    }

    /// <summary>Boundary: confirming with nothing waiting does nothing.</summary>
    [Fact]
    public async Task ConfirmDeleteAsync_NothingPending_DeletesNothing()
    {
        // Arrange
        var viewModel = Build(out var client, out _);

        // Act
        await viewModel.ConfirmDeleteAsync(TestContext.Current.CancellationToken);

        // Assert
        await client.DidNotReceive().DeleteModelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Positive: a model that is a default AI model names the purpose for the warning, with or without the tag.</summary>
    [Fact]
    public async Task DefaultPurposesOf_ModelThatIsADefault_NamesThePurposes()
    {
        // Arrange
        var viewModel = Build(out _, out var defaults);
        defaults.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<AiDefaultDto>>.Success(
        [
            new AiDefaultDto("website", "phi3.5", "manager", At, null),
            new AiDefaultDto("market_analysis", "llama3.2:3b", "manager", At, null),
        ]));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        // Act and assert
        Assert.Equal(["the website"], viewModel.DefaultPurposesOf("phi3.5:latest"));
        Assert.Equal(["the market analysis"], viewModel.DefaultPurposesOf("llama3.2:3b"));
        Assert.Empty(viewModel.DefaultPurposesOf("qwen2.5:7b"));
    }

    #endregion

    #region Helpers

    private static BenchmarkModelsViewModel Build(out IBenchmarkServiceClient client, out IAiDefaultService defaults)
    {
        var fake = Substitute.For<IBenchmarkServiceClient>();
        fake.ListModelsAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<string>>.Success(["llama3.2:3b", "phi3.5:latest"]));
        fake.StartModelPullAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<ModelPullDto>.Success(new ModelPullDto("pull-1", call.ArgAt<string>(0), ModelPullStates.Queued, null, null)));
        fake.DeleteModelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        client = fake;

        var fakeDefaults = Substitute.For<IAiDefaultService>();
        fakeDefaults.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<AiDefaultDto>>.Success([new AiDefaultDto("website", "llama3.2:3b", "manager", At, null)]));
        defaults = fakeDefaults;

        var clientRunner = Substitute.For<IUseCaseRunner<IBenchmarkServiceClient>>();
        clientRunner.RunAsync(
                Arg.Any<Func<IBenchmarkServiceClient, CancellationToken, Task<Result<IReadOnlyList<string>>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<IBenchmarkServiceClient, CancellationToken, Task<Result<IReadOnlyList<string>>>>>()(fake, call.Arg<CancellationToken>()));
        clientRunner.RunAsync(
                Arg.Any<Func<IBenchmarkServiceClient, CancellationToken, Task<Result<ModelPullDto>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<IBenchmarkServiceClient, CancellationToken, Task<Result<ModelPullDto>>>>()(fake, call.Arg<CancellationToken>()));
        clientRunner.RunAsync(
                Arg.Any<Func<IBenchmarkServiceClient, CancellationToken, Task<Result>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<IBenchmarkServiceClient, CancellationToken, Task<Result>>>()(fake, call.Arg<CancellationToken>()));

        var defaultsRunner = Substitute.For<IUseCaseRunner<IAiDefaultService>>();
        defaultsRunner.RunAsync(
                Arg.Any<Func<IAiDefaultService, CancellationToken, Task<Result<IReadOnlyList<AiDefaultDto>>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<IAiDefaultService, CancellationToken, Task<Result<IReadOnlyList<AiDefaultDto>>>>>()(fakeDefaults, call.Arg<CancellationToken>()));

        return new BenchmarkModelsViewModel(clientRunner, defaultsRunner);
    }

    #endregion
}
