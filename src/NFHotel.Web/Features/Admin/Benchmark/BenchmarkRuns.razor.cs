using System.Globalization;

using NFHotel.Application.Dto.Benchmark;
using NFHotel.Domain.Entities;
using NFHotel.Web.Common;
using NFHotel.Web.Components.Shared;

namespace NFHotel.Web.Features.Admin.Benchmark;

/// <summary>
/// The code behind the list of benchmark runs: the default models, the download that is followed, and the texts of
/// a run.
/// </summary>
public partial class BenchmarkRuns : CancellableComponentBase
{
    /// <summary>The benchmark pages as tabs: one menu item, same routes.</summary>
    private static readonly AdminTab[] BenchmarkTabs =
    [
        new("/admin/benchmark", "Runs"),
        new("/admin/benchmark/start", "Start a run"),
    ];

    /// <summary>Loads the list when the page opens.</summary>
    protected override Task OnInitializedAsync() => ViewModel.InitializeAsync(ComponentToken);

    private static readonly string[] Purposes = [AiDefault.Website, AiDefault.MarketAnalysis];

    private static string Title(string purpose) =>
        purpose == AiDefault.Website ? "Website" : "Market analysis";

    private async Task SaveDefaultAsync(string purpose)
    {
        await ViewModel.SaveDefaultAsync(purpose, ComponentToken);
        await FollowDownloadAsync();
    }

    private async Task SetDefaultAsync(string purpose, string model, Guid runId)
    {
        await ViewModel.SetDefaultAsync(purpose, model, runId, ComponentToken);
        await FollowDownloadAsync();
    }

    /// <summary>
    /// Reads the download of a chosen default model every few seconds until it ends; the view model saves the default
    /// when the model is installed and saves nothing when the download fails.
    /// </summary>
    private async Task FollowDownloadAsync()
    {
        try
        {
            while (ViewModel.IsDownloadingDefault && !ComponentToken.IsCancellationRequested)
            {
                await InvokeAsync(StateHasChanged);
                await Task.Delay(PullPollInterval, ComponentToken);
                await ViewModel.FollowDefaultDownloadAsync(ComponentToken);
            }
        }
        catch (OperationCanceledException)
        {
            // The page was closed: there is nobody to show the state to.
        }
    }

    private static readonly TimeSpan PullPollInterval = TimeSpan.FromSeconds(3);

    private Task ConfirmDeleteAsync() => ViewModel.ConfirmDeleteAsync(ComponentToken);

    private Task SelectAsync(Guid runId) => ViewModel.SelectAsync(runId, ComponentToken);

    private Task ShowOutputAsync(int benchmarkResultId) => ViewModel.ShowOutputAsync(benchmarkResultId, ComponentToken);

    private static string Machine(BenchmarkRunListItemDto run) =>
        $"{run.Cpu}, {run.MemoryMb / 1024d:0.#} GiB, {(string.IsNullOrWhiteSpace(run.Gpu) ? "CPU only" : run.Gpu)}";

    private static string TaskName(BenchmarkResultDto result) => BenchmarkResultAnalysis.TaskName(result);

    private static string Number(double? value) =>
        value is { } number ? number.ToString("0.##", CultureInfo.InvariantCulture) : "—";

    private static string Outcome(BenchmarkResultDto result) =>
        result.HasError ? "error"
        : result.Passed is { } passed ? (passed ? "passed" : "failed")
        : result.ResponseValid is { } valid ? (valid ? "valid answer" : "invalid answer")
        : "—";
}
