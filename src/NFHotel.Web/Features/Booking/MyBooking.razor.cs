using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

using Microsoft.AspNetCore.Components;

using NFHotel.Application.Dto.Bookings;
using NFHotel.Web.Common;
using NFHotel.Web.Security;

namespace NFHotel.Web.Features.Booking;

/// <summary>
/// The code behind the guest's own booking: the guest cookie, the hand-over of its token to the runner, and the
/// forwarding of the two forms to <see cref="MyBookingViewModel"/> (MIL-018, ADR-0049).
/// </summary>
public partial class MyBooking : CancellableComponentBase
{
    #region Fields

    /// <summary>The name of the cancel form, which the post carries in <c>_handler</c>.</summary>
    private const string CancelForm = "guest-cancel";

    /// <summary>The name of the form with the new dates.</summary>
    private const string ChangeForm = "guest-reschedule";

    #endregion Fields

    #region Properties

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    /// <summary>The result of the last change, which the address carries after a change succeeded.</summary>
    [SupplyParameterFromQuery(Name = "done")]
    private string? Done { get; set; }

    [SupplyParameterFromForm(FormName = ChangeForm)]
    private ChangeInput? Change { get; set; }

    #endregion Properties

    #region Protected methods

    /// <summary>
    /// Reads the guest cookie: without a guest the page goes to the sign-in; with one, the token goes to the runner of
    /// this request, and the API reads the booking that the token names.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        ClaimsPrincipal? guest = await GuestSignInRegistration.ReadGuestAsync(HttpContext);
        if (GuestClaims.TokenOf(guest) is null)
        {
            Navigation.NavigateTo(GuestSignInRegistration.SignInPath);
            return;
        }

        GuestTokens.Hold(guest);
        await ViewModel.LoadAsync(Done, ComponentToken);
        Change ??= ViewModel.Receipt is BookingReceiptDto receipt
            ? new ChangeInput { CheckIn = receipt.StartDate, CheckOut = receipt.EndDate }
            : new ChangeInput();
    }

    #endregion Protected methods

    #region Private methods

    private async Task CancelAsync()
    {
        if (await ViewModel.CancelAsync(ComponentToken))
        {
            Navigation.NavigateTo($"{GuestSignInRegistration.MyBookingPath}?done={MyBookingViewModel.CancelledResult}");
        }
    }

    private async Task RescheduleAsync()
    {
        if (Change is { CheckIn: DateOnly checkIn, CheckOut: DateOnly checkOut }
            && await ViewModel.RescheduleAsync(checkIn, checkOut, ComponentToken))
        {
            Navigation.NavigateTo($"{GuestSignInRegistration.MyBookingPath}?done={MyBookingViewModel.RescheduledResult}");
        }
    }

    #endregion Private methods

    #region Nested types

    /// <summary>The two dates of the change form.</summary>
    private sealed class ChangeInput
    {
        [Required(ErrorMessage = "Choose the new check-in date.")]
        public DateOnly? CheckIn { get; set; }

        [Required(ErrorMessage = "Choose the new check-out date.")]
        public DateOnly? CheckOut { get; set; }
    }

    #endregion Nested types
}
