using System.Globalization;

using Microsoft.AspNetCore.Components;

using NFHotel.Application.Dto.Benchmark;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Benchmark;

/// <summary>The code behind the start of a benchmark run: the polling of the run and the texts of its state.</summary>
public partial class BenchmarkStart : CancellableComponentBase
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);

    /// <summary>
    /// A recorded run whose choice is taken when the page opens (the list of runs links here with it), as the address
    /// spells it. Text: a value that is not the id of a run is ignored, so it cannot make the page answer an error page
    /// (MIL-067).
    /// </summary>
    [SupplyParameterFromQuery(Name = "again")]
    public string? AgainQuery { get; set; }

    /// <summary>Loads the installed models and the recorded runs, and then keeps them live.</summary>
    protected override async Task OnInitializedAsync()
    {
        await ViewModel.OpenAsync(Guid.TryParse(AgainQuery, out Guid again) ? again : null, ComponentToken);
        _ = PollAsync();
    }

    /// <summary>Offers the new list of models when a model was added or deleted in the panel.</summary>
    private Task ReloadModelsAsync() => ViewModel.InitializeAsync(ComponentToken);

    private async Task StartAsync()
    {
        await ViewModel.StartAsync(ComponentToken);
        await ViewModel.RefreshLiveAsync(ComponentToken);
    }

    /// <summary>
    /// Reads the runs and the models every few seconds for as long as the page is open (ADR-0006 of the benchmark service),
    /// so a change that any caller made shows without a refresh. The run that this page started is read on its own too.
    /// </summary>
    private async Task PollAsync()
    {
        try
        {
            while (!ComponentToken.IsCancellationRequested)
            {
                await Task.Delay(PollInterval, ComponentToken);
                await ViewModel.RefreshLiveAsync(ComponentToken);
                if (ViewModel.IsFollowing)
                {
                    await ViewModel.RefreshAsync(ComponentToken);
                }

                await InvokeAsync(StateHasChanged);
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

    private static string SinceText(BenchmarkRunStateDto run) =>
        run.StartedAt is { } started && !run.IsFinished ? $" since {Time(started)}" : string.Empty;

    private static string Time(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC";

    private static string Delivery(BenchmarkRunStateDto run) =>
        run.Delivery switch
        {
            BenchmarkDeliveryStates.Delivered => "stored",
            BenchmarkDeliveryStates.Pending => "waiting to be stored",
            BenchmarkDeliveryStates.Failed => "could not be stored (the benchmark service keeps it)",
            _ => "not ready",
        };
}
