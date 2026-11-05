using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Staff;

/// <summary>
/// The code behind the staff audit page: the number of pages and the paging, forwarded to <see
/// cref="StaffAuditViewModel"/>.
/// </summary>
public partial class StaffAudit : CancellableComponentBase
{
    private int Pages => Math.Max(1, (ViewModel.Total + StaffAuditViewModel.PageSize - 1) / StaffAuditViewModel.PageSize);

    /// <summary>Loads the first page of the trail when the page opens.</summary>
    protected override Task OnInitializedAsync() => ViewModel.LoadAsync(1, ComponentToken);

    private Task PreviousAsync() => ViewModel.LoadAsync(ViewModel.Page - 1, ComponentToken);

    private Task NextAsync() => ViewModel.LoadAsync(ViewModel.Page + 1, ComponentToken);
}
