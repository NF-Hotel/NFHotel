using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;

using NFHotel.Application.Dto.Staff;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Web.Common;
using NFHotel.Web.Security;

namespace NFHotel.Web.Features.Admin.Login;

/// <summary>
/// The code behind the sign-in page: the form, the sign-in use case and the cookie that is set on success.
/// </summary>
public partial class Login : CancellableComponentBase
{
    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    [SupplyParameterFromQuery(Name = "returnUrl")]
    private string? ReturnTo { get; set; }

    [SupplyParameterFromForm(FormName = "staff-login")]
    private LoginInput? Input { get; set; }

    /// <summary>Gives the form an empty input the first time the page is rendered.</summary>
    protected override void OnInitialized() => Input ??= new();

    /// <summary>
    /// Takes the password out of the form before the sign-in is asked, so the page never writes it back, whatever the
    /// answer (MIL-067).
    /// </summary>
    private async Task SignInAsync()
    {
        string password = Input?.Password ?? string.Empty;
        ForgetPassword();
        if (HttpContext is null || Input is null)
        {
            return;
        }

        StaffSessionDto? session = await ViewModel.SignInAsync(Input.UserName, password, ComponentToken);
        if (session is null)
        {
            return;
        }

        await HttpContext.SignInAsync(
            StaffClaims.CookieScheme,
            StaffClaims.CreatePrincipal(session),
            new AuthenticationProperties { ExpiresUtc = session.ExpiresAt, IsPersistent = false, AllowRefresh = false });

        Navigation.NavigateTo(ReturnUrl.Safe(ReturnTo, RolePermissions.LandingPath(session.Role)));
    }

    /// <summary>
    /// Empties the password of the form. The field writes its value into the page, so a form that is refused, such as one
    /// without a user name, would otherwise send the typed password back to the browser (MIL-067).
    /// </summary>
    private void ForgetPassword()
    {
        if (Input is not null)
        {
            Input.Password = string.Empty;
        }
    }

    private sealed class LoginInput
    {
        [Required(ErrorMessage = "Enter your user name.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your password.")]
        public string Password { get; set; } = string.Empty;
    }
}
