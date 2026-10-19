using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NFHotel.Application.Dto.AiDefaults;
using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.AiDefaults;
using NFHotel.Web.Common;
using NFHotel.Web.Features.Admin.Benchmark;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>The panel of installed models, rendered to HTML without a browser (MIL-020).</summary>
public sealed class ModelsPanelTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);

    /// <summary>Positive: every installed model has a Delete button, a default is badged, and the Add field is there.</summary>
    [Fact]
    public async Task ModelsPanel_InstalledModels_ListsThemWithDeleteButtonsAndDefaultBadges()
    {
        // Arrange and act
        var html = await RenderAsync(viewModel => Task.CompletedTask);

        // Assert
        Assert.Contains("llama3.2:3b", html, StringComparison.Ordinal);
        Assert.Contains("phi3.5:latest", html, StringComparison.Ordinal);
        Assert.Equal(2, Count(html, "btn btn-danger btn-sm"));
        Assert.Contains("default for the website", html, StringComparison.Ordinal);
        Assert.Contains("id=\"new-model-name\"", html, StringComparison.Ordinal);
        Assert.Contains("placeholder=\"phi4-mini\"", html, StringComparison.Ordinal);
    }

    /// <summary>Positive: a running download shows its model and percent as a progress bar.</summary>
    [Fact]
    public async Task ModelsPanel_RunningDownload_ShowsThePercentAndTheProgressBar()
    {
        // Arrange and act
        var html = await RenderAsync(async viewModel =>
        {
            viewModel.NewModelName = "phi4-mini";
            await viewModel.StartPullAsync();
        });

        // Assert
        Assert.Contains("Adding", html, StringComparison.Ordinal);
        Assert.Contains("phi4-mini", html, StringComparison.Ordinal);
        Assert.Contains("waiting", html, StringComparison.Ordinal);
    }

    /// <summary>Negative: a failed download shows the reason of Ollama as an error.</summary>
    [Fact]
    public async Task ModelsPanel_FailedDownload_ShowsTheReason()
    {
        // Arrange and act
        var html = await RenderAsync(async viewModel =>
        {
            viewModel.NewModelName = "nope";
            await viewModel.StartPullAsync();
            await viewModel.RefreshPullAsync();
        }, pullAfterStart: new ModelPullDto("pull-1", "nope", ModelPullStates.Failed, 40, "file does not exist"));

        // Assert
        Assert.Contains("could not be added: file does not exist", html, StringComparison.Ordinal);
    }

    /// <summary>Positive: asking to delete a default model shows the confirmation with the warning about the default.</summary>
    [Fact]
    public async Task ModelsPanel_DeleteOfADefaultModel_AsksWithTheWarning()
    {
        // Arrange and act
        var html = await RenderAsync(async viewModel =>
        {
            await viewModel.LoadAsync();
            viewModel.RequestDelete("llama3.2:3b");
        });

        // Assert
        Assert.Contains("Delete the model llama3.2:3b?", html, StringComparison.Ordinal);
        Assert.Contains("default AI model of the website", html, StringComparison.Ordinal);
        Assert.Contains("Delete model", html, StringComparison.Ordinal);
    }

    /// <summary>Positive: a download that is active in the service is shown with its percent when the panel opens.</summary>
    [Fact]
    public async Task ModelsPanel_ActiveDownloadInTheService_ShowsItsProgressAfterARefresh()
    {
        // Arrange and act
        var html = await RenderAsync(
            async viewModel => await viewModel.RefreshLiveAsync(),
            activePulls: [new ModelPullDto("pull-9", "phi4-mini", ModelPullStates.Pulling, 12, null)]);

        // Assert
        Assert.Contains("Adding", html, StringComparison.Ordinal);
        Assert.Contains("12 %", html, StringComparison.Ordinal);
        Assert.Contains("<progress", html, StringComparison.Ordinal);
    }

    private static int Count(string text, string part) => text.Split(part).Length - 1;

    private static async Task<string> RenderAsync(Func<BenchmarkModelsViewModel, Task> arrange, ModelPullDto? pullAfterStart = null, IReadOnlyList<ModelPullDto>? activePulls = null)
    {
        var client = Substitute.For<IBenchmarkServiceClient>();
        client.ListModelsAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<string>>.Success(["llama3.2:3b", "phi3.5:latest"]));
        client.StartModelPullAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Result<ModelPullDto>.Success(new ModelPullDto("pull-1", call.ArgAt<string>(0), ModelPullStates.Queued, null, null)));
        if (pullAfterStart is not null)
        {
            client.GetModelPullAsync("pull-1", Arg.Any<CancellationToken>()).Returns(Result<ModelPullDto>.Success(pullAfterStart));
        }

        client.ListModelPullsAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<ModelPullDto>>.Success(activePulls ?? []));

        var defaults = Substitute.For<IAiDefaultService>();
        defaults.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<AiDefaultDto>>.Success([new AiDefaultDto("website", "llama3.2:3b", "manager", At, null)]));

        var clientRunner = Substitute.For<IUseCaseRunner<IBenchmarkServiceClient>>();
        clientRunner.RunAsync(Arg.Any<Func<IBenchmarkServiceClient, CancellationToken, Task<Result<IReadOnlyList<string>>>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<IBenchmarkServiceClient, CancellationToken, Task<Result<IReadOnlyList<string>>>>>()(client, call.Arg<CancellationToken>()));
        clientRunner.RunAsync(Arg.Any<Func<IBenchmarkServiceClient, CancellationToken, Task<Result<ModelPullDto>>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<IBenchmarkServiceClient, CancellationToken, Task<Result<ModelPullDto>>>>()(client, call.Arg<CancellationToken>()));
        clientRunner.RunAsync(Arg.Any<Func<IBenchmarkServiceClient, CancellationToken, Task<Result<IReadOnlyList<ModelPullDto>>>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<IBenchmarkServiceClient, CancellationToken, Task<Result<IReadOnlyList<ModelPullDto>>>>>()(client, call.Arg<CancellationToken>()));
        var defaultsRunner = Substitute.For<IUseCaseRunner<IAiDefaultService>>();
        defaultsRunner.RunAsync(Arg.Any<Func<IAiDefaultService, CancellationToken, Task<Result<IReadOnlyList<AiDefaultDto>>>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<IAiDefaultService, CancellationToken, Task<Result<IReadOnlyList<AiDefaultDto>>>>>()(defaults, call.Arg<CancellationToken>()));

        var viewModel = new BenchmarkModelsViewModel(clientRunner, defaultsRunner);
        await arrange(viewModel);

        await using var services = new ServiceCollection().AddLogging().AddSingleton(viewModel).BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<ModelsPanel>(ParameterView.Empty);
            return output.ToHtmlString();
        });
    }
}
