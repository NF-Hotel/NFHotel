using Microsoft.AspNetCore.Components;

using NFHotel.Application.Dto.Guests;
using NFHotel.Web.Common;
using NFHotel.Web.Security;

namespace NFHotel.Web.Features.Booking;

/// <summary>
/// The code behind the page of the link in the mail of a booking request (MIL-088, ADR-0052 decision 3): the form, the
/// guest cookie that is set when the room is booked, and the way to the guest's own booking. The verification itself is
/// in <see cref="BookingVerifyViewModel"/>.
/// </summary>
public partial class BookingVerify : CancellableComponentBase
{
    #region Fields

    /// <summary>The name of the form, which the post carries in <c>_handler</c>.</summary>
    private const string FormName = "booking-verify";

    /// <summary>The name of the hidden field that carries the token; the script of the page fills it.</summary>
    private const string TokenField = "token";

    #endregion Fields

    #region Properties

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    /// <summary>The token of the post, from the body; never from the address.</summary>
    [SupplyParameterFromForm(Name = TokenField, FormName = FormName)]
    private string? Token { get; set; }

    #endregion Properties

    #region Private methods

    /// <summary>
    /// Verifies the request of the link, and when the room is booked sets the guest cookie, which expires with the guest
    /// token, and goes to the guest's own booking, which shows the receipt with the reference (ADR-0052 decision 5).
    /// </summary>
    private async Task VerifyAsync()
    {
        if (HttpContext is null)
        {
            return;
        }

        GuestSessionDto? session = await ViewModel.VerifyAsync(Token, ComponentToken);
        if (session is null)
        {
            return;
        }

        await GuestSignInRegistration.SignInAsync(HttpContext, session);
        Navigation.NavigateTo($"{GuestSignInRegistration.MyBookingPath}?done={MyBookingViewModel.BookedResult}");
    }

    #endregion Private methods
}
