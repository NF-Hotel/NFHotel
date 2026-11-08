using Microsoft.AspNetCore.Components;

using NFHotel.Web.Security;

namespace NFHotel.Web.Components.Layout;

/// <summary>The code behind the sign-in layout: the theme the user chose.</summary>
public partial class AdminAuthLayout : LayoutComponentBase
{
    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    private string? Theme => AdminTheme.Read(HttpContext);
}
