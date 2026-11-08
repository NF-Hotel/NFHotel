using Microsoft.AspNetCore.Components;

namespace NFHotel.Web.Components.Layout;

/// <summary>The code behind the side menu: which item is the current page.</summary>
public partial class AdminNav : ComponentBase
{
    private string Path => "/" + Navigation.ToBaseRelativePath(Navigation.Uri);

    private bool IsActive(string prefix, bool exact) =>
        exact
            ? Path.Split('?', 2)[0].TrimEnd('/').Equals(prefix, StringComparison.OrdinalIgnoreCase)
            : AdminRoutes.IsUnder(Path, prefix);

    /// <summary>The classes of a menu item; .active is the mark NavLink would have set.</summary>
    private string LinkClass(string prefix, bool exact = false) =>
        IsActive(prefix, exact) ? "admin-nav-link active" : "admin-nav-link";

    /// <summary>aria-current="page" on the current item, nothing on the rest.</summary>
    private string? Current(string prefix, bool exact = false) =>
        IsActive(prefix, exact) ? "page" : null;
}
