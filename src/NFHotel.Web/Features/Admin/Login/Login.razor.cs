using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;

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

    private async Task SignInAsync()
    {
        if (HttpContext is null || Input is null)
        {
            return;
        }

        var session = await ViewModel.SignInAsync(Input.UserName, Input.Password, ComponentToken);
        Input.Password = string.Empty;
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

    private sealed class LoginInput
    {
        [Required(ErrorMessage = "Enter your user name.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your password.")]
        public string Password { get; set; } = string.Empty;
    }
}
