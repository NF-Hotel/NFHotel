using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Components;

using NFHotel.Application.Dto.Guests;
using NFHotel.Web.Common;
using NFHotel.Web.Security;

namespace NFHotel.Web.Features.Booking;

/// <summary>
/// The code behind the guest sign-in: the form and the guest cookie that is set on success (MIL-018, ADR-0049). The
/// sign-in itself is in <see cref="GuestSignInViewModel"/>.
/// </summary>
public partial class GuestSignIn : CancellableComponentBase
{
    #region Fields

    /// <summary>The name of the form, which the post carries in <c>_handler</c>.</summary>
    private const string FormName = "guest-sign-in";

    #endregion Fields

    #region Properties

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    [SupplyParameterFromForm(FormName = FormName)]
    private GuestSignInInput? Input { get; set; }

    #endregion Properties

    #region Protected methods

    /// <summary>Gives the page an empty form the first time it is rendered.</summary>
    protected override void OnInitialized() => Input ??= new GuestSignInInput();

    #endregion Protected methods

    #region Private methods

    /// <summary>
    /// Asks the sign-in, and on success sets the guest cookie, which expires with the guest token, and goes to the
    /// guest's own booking.
    /// </summary>
    private async Task SignInAsync()
    {
        if (HttpContext is null || Input is null)
        {
            return;
        }

        GuestSessionDto? session = await ViewModel.SignInAsync(Input.Reference, Input.Email, ComponentToken);
        if (session is null)
        {
            return;
        }

        await GuestSignInRegistration.SignInAsync(HttpContext, session);
        Navigation.NavigateTo(GuestSignInRegistration.MyBookingPath);
    }

    #endregion Private methods

    #region Nested types

    /// <summary>The two fields of the form.</summary>
    private sealed class GuestSignInInput
    {
        [Required(ErrorMessage = "Enter your booking reference.")]
        public string Reference { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter the email you booked with.")]
        public string Email { get; set; } = string.Empty;
    }

    #endregion Nested types
}
