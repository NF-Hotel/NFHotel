using Microsoft.AspNetCore.Components;

namespace NFHotel.Web.Components.Shared;

/// <summary>The code behind the refusal of a page: where the role starts instead.</summary>
public partial class NotPermitted : ComponentBase
{
    /// <summary>The page the role starts on.</summary>
    [Parameter]
    [EditorRequired]
    public string Landing { get; set; } = "/";
}
