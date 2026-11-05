using NFHotel.Application.Dto.Staff;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Staff;

/// <summary>
/// The code behind the staff accounts page: the state text of an account and the forwarding to <see
/// cref="StaffListViewModel"/>.
/// </summary>
public partial class StaffList : CancellableComponentBase
{
    /// <summary>Loads the accounts when the page opens.</summary>
    protected override Task OnInitializedAsync() => ViewModel.LoadAsync(ComponentToken);

    private static string StateOf(StaffAccountDto account) =>
        !account.IsActive ? "Disabled" : account.MustChangePassword ? "Must change password" : "Active";

    private Task CreateAsync() => ViewModel.CreateAsync(ComponentToken);

    private Task ConfirmDisableAsync() => ViewModel.ConfirmDisableAsync(ComponentToken);

    private Task EnableAsync(StaffAccountDto account) => ViewModel.EnableAsync(account, ComponentToken);

    private Task ResetAsync() => ViewModel.ConfirmResetAsync(ComponentToken);
}
