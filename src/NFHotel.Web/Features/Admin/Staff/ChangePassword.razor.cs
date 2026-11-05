using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;

using NFHotel.Application.UseCases.Staff;
using NFHotel.Web.Common;
using NFHotel.Web.Security;

namespace NFHotel.Web.Features.Admin.Staff;

/// <summary>
/// The code behind the change of the own password: the form, the use case and the cookie that is replaced on
/// success.
/// </summary>
public partial class ChangePassword : CancellableComponentBase
{
    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    private bool _mustChange;

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    [SupplyParameterFromForm(FormName = "change-password")]
    private ChangeInput? Input { get; set; }

    /// <summary>Gives the form an empty input, and reads from the cookie whether the password must be changed.</summary>
    protected override void OnInitialized()
    {
        Input ??= new();
        _mustChange = HttpContext is not null && StaffClaims.MustChangePassword(HttpContext.User);
    }

    private async Task ChangeAsync()
    {
        if (HttpContext is null || Input is null)
        {
            return;
        }

        var session = await ViewModel.ChangeAsync(Input.CurrentPassword, Input.NewPassword, ComponentToken);
        Input.CurrentPassword = string.Empty;
        Input.NewPassword = string.Empty;
        Input.RepeatPassword = string.Empty;
        if (session is null)
        {
            return;
        }

        await HttpContext.SignInAsync(
            StaffClaims.CookieScheme,
            StaffClaims.CreatePrincipal(session),
            new AuthenticationProperties { ExpiresUtc = session.ExpiresAt, IsPersistent = false, AllowRefresh = false });

        Navigation.NavigateTo(RolePermissions.LandingPath(session.Role));
    }

    private sealed class ChangeInput
    {
        [Required(ErrorMessage = "Enter your current password.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a new password.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Repeat the new password.")]
        [Compare(nameof(NewPassword), ErrorMessage = "The two new passwords are not the same.")]
        public string RepeatPassword { get; set; } = string.Empty;
    }
}
