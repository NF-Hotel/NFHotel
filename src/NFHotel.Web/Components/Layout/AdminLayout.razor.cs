using Microsoft.AspNetCore.Components;

using NFHotel.Web.Security;

namespace NFHotel.Web.Components.Layout;

/// <summary>The code behind the admin shell: the theme the user chose.</summary>
public partial class AdminLayout : LayoutComponentBase
{
    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    private string? Theme => AdminTheme.Read(HttpContext);
}
