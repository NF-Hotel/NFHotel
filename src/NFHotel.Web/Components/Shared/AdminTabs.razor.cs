using Microsoft.AspNetCore.Components;

using NFHotel.Web.Components.Layout;

namespace NFHotel.Web.Components.Shared;

/// <summary>The code behind the tabs of sibling pages: which tab is the current page.</summary>
public partial class AdminTabs : ComponentBase
{
    /// <summary>The tabs, in order.</summary>
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<AdminTab> Tabs { get; set; } = Array.Empty<AdminTab>();

    /// <summary>The name of the area, read out as the label of the tab list.</summary>
    [Parameter]
    public string Label { get; set; } = "Pages";

    private bool IsCurrent(AdminTab tab)
    {
        string path = "/" + Navigation.ToBaseRelativePath(Navigation.Uri);
        return AdminRoutes.IsUnder(path, tab.Href);
    }
}
