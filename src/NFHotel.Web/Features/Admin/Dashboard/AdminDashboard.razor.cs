using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Dashboard;

/// <summary>
/// The code behind the dashboard: the subheading and the forwarding to <see cref="AdminDashboardViewModel"/>.
/// </summary>
public partial class AdminDashboard : CancellableComponentBase
{
    /// <summary>The subheading with today's date.</summary>
    private string SubtitleText => $"Where things stand on {AdminFormat.Date(ViewModel.Today)}";

    /// <summary>Fetches today's figures when the screen opens.</summary>
    protected override Task OnInitializedAsync() => ViewModel.InitializeAsync(ComponentToken);

    /// <summary>Passes the refresh button on to the viewmodel.</summary>
    private Task RefreshAsync() => ViewModel.RefreshAsync(ComponentToken);
}
