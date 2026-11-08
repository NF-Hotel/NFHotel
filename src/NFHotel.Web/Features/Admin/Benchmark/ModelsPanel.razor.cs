using Microsoft.AspNetCore.Components;

using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Benchmark;

/// <summary>
/// The code behind the models panel: the polling of the downloads and the forwarding to <see
/// cref="BenchmarkModelsViewModel"/>.
/// </summary>
public partial class ModelsPanel : CancellableComponentBase
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);

    /// <summary>Called when the list of installed models changed, so the page can offer the new list.</summary>
    [Parameter]
    public EventCallback OnModelsChanged { get; set; }

    /// <summary>Loads the models and the defaults when the panel opens, and then keeps them live.</summary>
    protected override async Task OnInitializedAsync()
    {
        await ViewModel.LoadAsync(ComponentToken);
        await ViewModel.RefreshLiveAsync(ComponentToken);
        _ = PollAsync();
    }

    private async Task StartPullAsync()
    {
        await ViewModel.StartPullAsync(ComponentToken);
        await ViewModel.RefreshLiveAsync(ComponentToken);
    }

    private async Task ConfirmDeleteAsync()
    {
        await ViewModel.ConfirmDeleteAsync(ComponentToken);
        await OnModelsChanged.InvokeAsync();
    }

    /// <summary>
    /// Reads the downloads and the models every few seconds for as long as the panel is open (ADR-0006 of the benchmark
    /// service), so a download that was started elsewhere, or before a refresh, shows its progress without a click.
    /// </summary>
    private async Task PollAsync()
    {
        try
        {
            while (!ComponentToken.IsCancellationRequested)
            {
                await Task.Delay(PollInterval, ComponentToken);
                var wasPulling = ViewModel.IsPulling;
                var changed = await ViewModel.RefreshLiveAsync(ComponentToken);
                if (ViewModel.IsPulling)
                {
                    await ViewModel.RefreshPullAsync(ComponentToken);
                }

                await InvokeAsync(StateHasChanged);
                if (changed || (wasPulling && !ViewModel.IsPulling))
                {
                    await OnModelsChanged.InvokeAsync();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // The page was closed: there is nobody to show the state to.
        }
        catch (ObjectDisposedException)
        {
            // The circuit ended between the answer and the redraw: the same case, the page is gone.
        }
    }

    private static string DeleteMessage(IReadOnlyList<string> purposes) =>
        purposes.Count == 0
            ? "The model is removed from the server for good. A benchmark that is queued or running with it stops the delete."
            : $"This model is the default AI model of {string.Join(" and ", purposes)}. It is removed from the server for good and the default stays set, so choose another default afterwards.";
}
