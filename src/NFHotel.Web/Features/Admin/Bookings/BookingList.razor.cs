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

    /// <summary>Gives the button for the guest mode the right appearance class.</summary>
    private string GuestModeClass(BookingGuestMode mode) =>
        Form.GuestMode == mode ? "btn btn-primary btn-sm" : "btn btn-secondary btn-sm";

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
