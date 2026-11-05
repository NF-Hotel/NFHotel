using System.Globalization;

using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.MarketAnalysis;

/// <summary>
/// The code behind the market analysis report: the polling of a run that is going on, and the texts of its state.
/// </summary>
public partial class MarketAnalysisReport : CancellableComponentBase
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);

    /// <summary>Loads the stored report when the page opens, and then follows a run that is going on, whoever started it.</summary>
    protected override async Task OnInitializedAsync()
    {
        await ViewModel.InitializeAsync(ComponentToken);
        _ = PollAsync();
    }

    private async Task StartAsync()
    {
        await ViewModel.StartAsync(ComponentToken);
    }

    /// <summary>Reads the state of the followed run every few seconds for as long as the page is open.</summary>
    private async Task PollAsync()
    {
        try
        {
            while (!ComponentToken.IsCancellationRequested)
            {
                await Task.Delay(PollInterval, ComponentToken);
                if (ViewModel.IsFollowing)
                {
                    await ViewModel.RefreshAsync(ComponentToken);
                    await InvokeAsync(StateHasChanged);
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

    private static string[] Paragraphs(string text) =>
        text.Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string SinceText(MarketAnalysisRunStateDto run) =>
        run.StartedAt is { } started ? $" since {Time(started)}" : string.Empty;

    private static string Time(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC";
}
