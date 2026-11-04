using Microsoft.AspNetCore.Components;

using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Booking;

/// <summary>
/// The code behind the booking flow. It contains only forwarding to <see cref="BookingFlowViewModel"/>,
/// the hand-over of the address to the viewmodel, and the one navigation that belongs to the browser.
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

    /// <summary>The number of guests from the front page's booking box or a room's "Check availability" link.</summary>
    [SupplyParameterFromQuery(Name = "guests")]
    public int? GuestsQuery { get; set; }

    /// <summary>The browser's navigation, for the one step that leaves the page: on to the receipt.</summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    /// <summary>The address of the bookmarkable receipt for the booking just created.</summary>
    private string ReceiptUrl =>
        ViewModel.Confirmation is null ? "/book" : $"/booking/{ViewModel.Confirmation.BookingNumber}";

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
    /// Sends the order and takes the customer on to the bookmarkable receipt.
    /// </summary>
    /// <remarks>
    /// The navigation is here and not in the viewmodel: a URL is a concept of the browser, not
    /// of the booking. Step 4 is nevertheless drawn first, so the receipt is on the screen even
    /// if the navigation should happen to fail.
    /// </remarks>
    private async Task SubmitAsync()
    {
        await ViewModel.SubmitAsync(ComponentToken);

        if (ViewModel.Confirmation is not null)
        {
            Navigation.NavigateTo(ReceiptUrl);
        }
    }
}
