using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Dashboard;

/// <summary>
/// The code behind the dashboard: the subheading and the forwarding to <see cref="AdminDashboardViewModel"/>.
/// </summary>
public partial class AdminDashboard : CancellableComponentBase
{
    /// <summary>The subheading with today's date and tonight's occupancy.</summary>
    private string SubtitleText => ViewModel.IsLoaded && ViewModel.RoomCount > 0
        ? $"{AdminFormat.Date(ViewModel.Today)} · {ViewModel.OccupiedTonight} of {ViewModel.RoomCount} rooms occupied tonight"
        : $"Where things stand on {AdminFormat.Date(ViewModel.Today)}";

    private string ArrivalsNote => ViewModel.Arrivals.Count == 0
        ? "Nobody is due today"
        : $"{Plural(ViewModel.Arrivals.Count - ViewModel.ArrivalsCheckedIn, "guest")} still to arrive";

    private string DeparturesNote => ViewModel.Departures.Count == 0
        ? "No room frees up today"
        : $"{Plural(ViewModel.Departures.Count - ViewModel.DeparturesCheckedOut, "room")} still to free up";

    private string CleaningNote => ViewModel.RoomsAwaitingCleaning.Count == 0
        ? "Every room is clean"
        : $"{Plural(ViewModel.RoomsNeedingService.Count, "room")} also needs service";

    private string OccupancyNote => $"{ViewModel.OccupiedTonight} of {ViewModel.RoomCount} rooms";

    /// <summary>Fetches today's figures when the screen opens.</summary>
    protected override Task OnInitializedAsync() => ViewModel.InitializeAsync(ComponentToken);

    /// <summary>Passes the refresh button on to the viewmodel.</summary>
    private Task RefreshAsync() => ViewModel.RefreshAsync(ComponentToken);

    /// <summary>"2 checked in" after a figure, or nothing when the count is zero.</summary>
    private static string? Checked(int count, string what) => count == 0 ? null : $"{count} {what}";

    /// <summary>"1 room" or "3 rooms".</summary>
    private static string Plural(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";
}
