using Microsoft.AspNetCore.Components;

namespace NFHotel.Web.Components.Shared;

/// <summary>The code behind the loading state: how many placeholder lines it draws.</summary>
public partial class LoadingSkeleton : ComponentBase
{
    /// <summary>Number of placeholder lines. Set it to match the content that is on its way.</summary>
    [Parameter]
    public int Rows { get; set; } = 3;
}
