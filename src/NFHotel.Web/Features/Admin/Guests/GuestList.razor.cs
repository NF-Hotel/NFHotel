using NFHotel.Application.Dto.Guests;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Guests;

/// <summary>
/// The code behind the guest overview: forwarding to <see cref="GuestListViewModel"/> with the token of the page.
/// </summary>
public partial class GuestList : CancellableComponentBase
{
    /// <summary>The heading in the confirmation of a deletion.</summary>
    private string DeleteTitle => $"Delete {ViewModel.GuestPendingDeletion?.FullName}?";

    /// <summary>Fetches the first list when the screen opens.</summary>
    protected override Task OnInitializedAsync() => ViewModel.InitializeAsync(ComponentToken);

    /// <summary>Says whether a passport number is registered, without showing it.</summary>
    private static string PassportText(GuestListItemDto guest) =>
        guest.HasPassportNumber ? "On file" : "—";

    private Task SearchAsync() => ViewModel.SearchAsync(ComponentToken);

    private Task ClearSearchAsync() => ViewModel.ClearSearchAsync(ComponentToken);

    private void Validate() => ViewModel.Validate();

    private void BeginCreate()
    {
        ViewModel.DismissSuccess();
        ViewModel.BeginCreate();
    }

    private Task BeginEditAsync(GuestListItemDto guest)
    {
        ViewModel.DismissSuccess();

        return ViewModel.BeginEditAsync(guest.GuestId, ComponentToken);
    }

    private void CancelEdit() => ViewModel.CancelEdit();

    private Task SaveAsync() => ViewModel.SaveAsync(ComponentToken);

    private void RequestDelete(GuestListItemDto guest)
    {
        ViewModel.DismissSuccess();
        ViewModel.RequestDelete(guest);
    }

    private void AbortDelete() => ViewModel.AbortDelete();

    private Task ConfirmDeleteAsync() => ViewModel.ConfirmDeleteAsync(ComponentToken);
}
