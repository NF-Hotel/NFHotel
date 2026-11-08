using Microsoft.AspNetCore.Components;

using NFHotel.Web.Security;

namespace NFHotel.Web.Components.Layout;

/// <summary>The code behind the top bar: where the user is, the next theme and the avatar.</summary>
public partial class AdminTopbar : ComponentBase
{
    /// <summary>The theme the user chose, or <c>null</c> when the shell follows the system.</summary>
    [Parameter]
    public string? Theme { get; set; }

    private string Path => "/" + Navigation.ToBaseRelativePath(Navigation.Uri);

    private AdminRoutes.Crumb Crumb => AdminRoutes.For(Path);

    private string NextTheme => Theme switch
    {
        AdminTheme.Dark => AdminTheme.Light,
        AdminTheme.Light => AdminTheme.System,
        _ => AdminTheme.Dark,
    };

    private string ThemeIcon => Theme switch
    {
        AdminTheme.Dark => "sun",
        AdminTheme.Light => "monitor",
        _ => "moon",
    };

    private string ThemeLabel => Theme switch
    {
        AdminTheme.Dark => "Switch to the light theme",
        AdminTheme.Light => "Follow the system theme",
        _ => "Switch to the dark theme",
    };

    /// <summary>The first two letters of the user name, for the avatar.</summary>
    private static string Initials(string? name) =>
        string.IsNullOrWhiteSpace(name) ? "?" : name.Trim()[..Math.Min(2, name.Trim().Length)].ToUpperInvariant();
}
