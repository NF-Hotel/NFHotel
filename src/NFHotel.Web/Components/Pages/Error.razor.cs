using System.Diagnostics;

using Microsoft.AspNetCore.Components;

namespace NFHotel.Web.Components.Pages;

/// <summary>The code behind the error page: the trace id of the request, and nothing else about the error.</summary>
public partial class Error : ComponentBase
{
    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    private string? RequestId { get; set; }

    private bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    /// <summary>Reads the trace id of the request, from the activity or from the request itself.</summary>
    protected override void OnInitialized() =>
        RequestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
}
