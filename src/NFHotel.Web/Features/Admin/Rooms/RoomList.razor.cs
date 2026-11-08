using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

using NFHotel.Application.Dto.Rooms;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;
using NFHotel.Web.Security;

namespace NFHotel.Web.Features.Admin.Rooms;

/// <summary>
/// The code behind the room overview: the filter options, the policies of the actions, and the forwarding to <see
/// cref="RoomListViewModel"/> and <see cref="RoomFormViewModel"/>.
/// </summary>
public partial class RoomList : CancellableComponentBase
{
    [Inject]
    private AuthenticationStateProvider AuthenticationState { get; set; } = null!;

    [Inject]
    private IAuthorizationService Authorization { get; set; } = null!;

    /// <summary>All room sizes, for the dropdown of the filter.</summary>
    private static readonly RoomSize[] Sizes = Enum.GetValues<RoomSize>();

    /// <summary>All operating statuses, for the dropdown of the filter.</summary>
    private static readonly RoomStatus[] RoomStatuses = Enum.GetValues<RoomStatus>();

    /// <summary>All cleaning statuses, for the dropdown of the filter.</summary>
    private static readonly HousekeepingStatus[] HousekeepingStatuses = Enum.GetValues<HousekeepingStatus>();

    /// <summary>The floors that actually exist, fetched with the filter options.</summary>
    private IReadOnlyList<int> Floors =>
        ViewModel.FilterOptions?.Floors ?? Array.Empty<int>();

    /// <summary>The heading in the confirmation of a deletion.</summary>
    private string DeleteTitle => $"Delete room {ViewModel.RoomPendingDeletion?.RoomNumber}?";

    /// <summary>
    /// Fetches filter options and the first list when the screen opens. The prices are asked for only when the user passes
    /// the same policy that shows the pricing links in the menu (MIL-055).
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        var state = await AuthenticationState.GetAuthenticationStateAsync();
        var prices = await Authorization.AuthorizeAsync(state.User, StaffPolicies.ManagePrices);

        await ViewModel.InitializeAsync(prices.Succeeded, ComponentToken);
    }

    /// <summary>The policy that decides whether the user sees the button of an action (ADR-0005).</summary>
    private static string PolicyFor(RoomAction action) =>
        action is RoomAction.TakeOutOfService or RoomAction.SendToMaintenance or RoomAction.ReturnToService
            ? StaffPolicies.RoomsManage
            : StaffPolicies.HousekeepingUpdate;

    /// <summary>The text on the button that opens and closes the action panel.</summary>
    private string SelectLabel(RoomListItemDto room) =>
        ViewModel.IsSelected(room) ? "Hide actions" : "Actions";

    private Task SearchAsync() => ViewModel.SearchAsync(ComponentToken);

    private Task ClearFilterAsync() => ViewModel.ClearFilterAsync(ComponentToken);

    private void ToggleSelection(RoomListItemDto room) => ViewModel.ToggleSelection(room);

    private Task ExecuteAsync(RoomAction action) => ViewModel.ExecuteAsync(action, ComponentToken);

    private void RequestDelete(RoomListItemDto room)
    {
        ViewModel.DismissSuccess();
        ViewModel.RequestDelete(room);
    }

    private void AbortDelete() => ViewModel.AbortDelete();

    private Task ConfirmDeleteAsync() => ViewModel.ConfirmDeleteAsync(ComponentToken);

    private void BeginCreate()
    {
        Form.DismissSuccess();
        ViewModel.DismissSuccess();
        Form.BeginCreate();
    }

    private Task BeginEditAsync(RoomListItemDto room)
    {
        Form.DismissSuccess();
        ViewModel.DismissSuccess();

        return Form.BeginEditAsync(room.RoomId, ComponentToken);
    }

    private void CloseForm() => Form.Close();

    /// <summary>
    /// Saves the form and fetches the list again if it succeeded.
    /// </summary>
    private async Task SaveAsync()
    {
        await Form.SaveAsync(ComponentToken);

        if (Form.SavedSuccessfully)
        {
            await ViewModel.SearchAsync(ComponentToken);
        }
    }

    /// <summary>The classes of a tile on the board: the colour of its frame follows the housekeeping badge.</summary>
    private string TileClass(RoomListItemDto room)
    {
        string state = room.Status != RoomStatus.Available
            ? "blocked"
            : room.HousekeepingStatus switch
            {
                HousekeepingStatus.Clean => "clean",
                HousekeepingStatus.CleaningInProgress => "in-progress",
                HousekeepingStatus.ServiceRequired or HousekeepingStatus.ServiceInProgress => "service",
                _ => "due",
            };

        return ViewModel.IsSelected(room) ? $"room-tile {state} is-selected" : $"room-tile {state}";
    }

    /// <summary>The short state under the room number: the operating state when it blocks the room, else the cleaning.</summary>
    private static string TileState(RoomListItemDto room) =>
        room.Status != RoomStatus.Available
            ? DisplayText.Describe(room.Status)
            : DisplayText.Describe(room.HousekeepingStatus);
}
