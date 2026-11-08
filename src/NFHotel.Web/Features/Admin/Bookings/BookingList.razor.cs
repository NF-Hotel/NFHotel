using NFHotel.Application.Dto.Bookings;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Bookings;

/// <summary>
/// The code behind the booking overview: forwarding to <see cref="BookingListViewModel"/> and <see
/// cref="BookingFormViewModel"/> with the token of the page.
/// </summary>
public partial class BookingList : CancellableComponentBase
{
    /// <summary>The statuses in the dropdown of the filter.</summary>
    private static readonly BookingStatus[] Statuses = Enum.GetValues<BookingStatus>();

    /// <summary>The text on the save button, which follows the state of the form.</summary>
    private string SaveLabel => Form.Mode == BookingFormMode.Create ? "Create booking" : "Save new dates";

    /// <summary>The heading in the confirmation of a cancellation.</summary>
    private string CancelTitle =>
        $"Cancel booking {ViewModel.BookingPendingCancellation?.BookingNumber}?";

    /// <summary>Fetches the first list when the screen opens.</summary>
    protected override Task OnInitializedAsync() => ViewModel.InitializeAsync(ComponentToken);

    /// <summary>One line under the title of the panel: how far the flow has come.</summary>
    private string FlowHint => Form.Mode == BookingFormMode.Reschedule
        ? "Pick the new dates and a room that is free for them."
        : Form.SelectedRoomId is null
            ? "Pick the dates, then a free room, then the guest."
            : Form.CanSave ? "Everything is chosen. Create the booking when it looks right." : "Now the guest: find one in the register or add a new one.";

    /// <summary>The classes of a step: done when its choice is made, active when it is the next thing to do.</summary>
    private string StepClass(int step)
    {
        bool datesDone = Form.HasSearchedRooms;
        bool roomDone = Form.SelectedRoomId is not null;
        bool guestDone = Form.GuestMode == BookingGuestMode.Existing ? Form.SelectedGuestId is not null : Form.NewGuestIsValid;
        bool done = step switch { 1 => datesDone, 2 => roomDone, _ => guestDone };
        bool active = step switch { 1 => !datesDone, 2 => datesDone && !roomDone, _ => roomDone && !guestDone };

        return done ? "step-block is-done" : active ? "step-block is-active" : "step-block";
    }

    /// <summary>Marks the chosen row of the rooms and the guests.</summary>
    private static string RowClass(bool selected) => selected ? "option-row is-selected" : "option-row";

    /// <summary>Writes aria-pressed as text; a bool would become an empty attribute.</summary>
    private string GuestModePressed(BookingGuestMode mode) =>
        Form.GuestMode == mode ? "true" : "false";

    private Task SearchAsync() => ViewModel.SearchAsync(ComponentToken);

    private Task ClearFilterAsync() => ViewModel.ClearFilterAsync(ComponentToken);

    private void ApplyStatusFilter() => ViewModel.FilterByStatus(ViewModel.StatusFilter);

    private void ShowAllStatuses() => ViewModel.FilterByStatus(null);

    private Task SortAsync(BookingSortField field) => ViewModel.SortByAsync(field, ComponentToken);

    private Task ExecuteAsync(BookingAction action, BookingListItemDto booking) =>
        ViewModel.ExecuteAsync(action, booking.BookingId, ComponentToken);

    private void RequestCancel(BookingListItemDto booking) => ViewModel.RequestCancel(booking);

    private void AbortCancel() => ViewModel.AbortCancel();

    private Task ConfirmCancelAsync() => ViewModel.ConfirmCancelAsync(ComponentToken);

    private void BeginCreate()
    {
        Form.DismissSuccess();
        Form.BeginCreate();
    }

    private Task BeginRescheduleAsync(BookingListItemDto booking)
    {
        Form.DismissSuccess();

        return Form.BeginRescheduleAsync(booking, ComponentToken);
    }

    private void CloseForm() => Form.Close();

    private Task SearchRoomsAsync() => Form.SearchRoomsAsync(ComponentToken);

    private Task SearchGuestsAsync() => Form.SearchGuestsAsync(ComponentToken);

    private void ValidateGuest() => Form.ValidateNewGuest();

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
}
