using Microsoft.AspNetCore.Components;

using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Booking;

/// <summary>
/// The code behind the booking flow. It contains only forwarding to <see cref="BookingFlowViewModel"/> and
/// the hand-over of the address to the viewmodel.
/// All state, all validation and all service calls are in the viewmodel (UI-Spec section 4, B-10).
/// </summary>
public partial class Book : CancellableComponentBase
{
    /// <summary>The check-in date from the front page's booking box, as the address spells it (<c>yyyy-MM-dd</c>).</summary>
    [SupplyParameterFromQuery(Name = "checkIn")]
    public string? CheckInQuery { get; set; }

    /// <summary>The check-out date from the front page's booking box.</summary>
    [SupplyParameterFromQuery(Name = "checkOut")]
    public string? CheckOutQuery { get; set; }

    /// <summary>
    /// The number of guests from the front page's booking box or a room's "Check availability" link, as the address
    /// spells it. Text, like the dates: the viewmodel reads it, so a value that is not a number cannot make the page
    /// answer an error page (MIL-067).
    /// </summary>
    [SupplyParameterFromQuery(Name = "guests")]
    public string? GuestsQuery { get; set; }

    /// <summary>
    /// Hands the address to the viewmodel. Valid dates go straight to the free rooms, so the guest who
    /// filled in the box on the front page is not asked the same question twice.
    /// </summary>
    protected override Task OnInitializedAsync() =>
        ViewModel.Prefill(CheckInQuery, CheckOutQuery, GuestsQuery)
            ? ViewModel.FindRoomsAsync(ComponentToken)
            : Task.CompletedTask;

    /// <summary>Marks an input as invalid for screen readers, or omits the attribute.</summary>
    private static string? Invalid(string? error) => error is null ? null : "true";

    private Task FindRoomsAsync() => ViewModel.FindRoomsAsync(ComponentToken);

    private void BackToDates() => ViewModel.BackToDates();

    private void BackToRooms() => ViewModel.BackToRooms();

    private void StartOver() => ViewModel.StartOver();

    /// <summary>
    /// Sends the order. The receipt is step 4 on this page: the guest who has just booked sees it at once, without
    /// the reference and the email that the guest sign-in on /booking asks for (MIL-079, MIL-018, ADR-0049).
    /// </summary>
    private Task SubmitAsync() => ViewModel.SubmitAsync(ComponentToken);
}
