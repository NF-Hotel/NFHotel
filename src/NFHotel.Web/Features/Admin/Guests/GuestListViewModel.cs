using NFHotel.Application.Dto.Guests;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Guests;

/// <summary>
/// Presentation logic for the guest overview: search as well as create, change and delete guest.
/// </summary>
/// <remarks>
/// B-10: the class validates <b>nothing</b> itself. The form asks
/// <see cref="IGuestService.ValidateFields"/> on every change, so BR-52 to BR-56 never
/// exist in the Web layer, and the save button is enabled by the answer of the domain rather than by a copy of
/// the rules. All validation errors are shown at the same time (BR-60, BR-96).
/// <para>
/// Deletion requires a confirmation first. The service rejects a guest with bookings (BR-N-02),
/// but a guest without bookings disappears for good — and the user must be given time to say no
/// to that.
/// </para>
/// </remarks>
public sealed class GuestListViewModel
{
    private readonly IUseCaseRunner<IGuestService> _guests;

    /// <summary>
    /// Opretter viewmodellen.
    /// </summary>
    /// <param name="guests">Runs use cases on <see cref="IGuestService"/>.</param>
    public GuestListViewModel(IUseCaseRunner<IGuestService> guests)
    {
        ArgumentNullException.ThrowIfNull(guests);

        _guests = guests;
    }

    /// <summary>The guests shown right now.</summary>
    public IReadOnlyList<GuestListItemDto> Guests { get; private set; } =
        Array.Empty<GuestListItemDto>();

    /// <summary>Errors from the list and from deletion, already translated to Danish.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    /// <summary>Errors from the form, already translated to Danish.</summary>
    public IReadOnlyList<string> FormErrors { get; private set; } = Array.Empty<string>();

    /// <summary>Receipt after a successful save or a deletion.</summary>
    public string? SuccessMessage { get; private set; }

    /// <summary>True while a call is in progress.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>True when the screen has fetched data at least once.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>True when the list was fetched without error, but is empty.</summary>
    public bool IsEmpty => IsLoaded && Errors.Count == 0 && Guests.Count == 0;

    /// <summary>True when the empty list is due to a search and not to an empty register.</summary>
    public bool IsEmptySearch => IsEmpty && !string.IsNullOrWhiteSpace(SearchText);

    /// <summary>Free text on name, country or email (BR-68, BR-N-05).</summary>
    public string? SearchText { get; set; }

    /// <summary>True when the form is open.</summary>
    public bool IsFormOpen { get; private set; }

    /// <summary>The id of the guest being changed, or <c>null</c> when a new one is created (BR-72).</summary>
    public int? EditingGuestId { get; private set; }

    /// <summary>Formularens overskrift.</summary>
    public string FormTitle => EditingGuestId is null ? "New guest" : "Edit guest";

    /// <summary>The guest waiting for the user to confirm a deletion.</summary>
    public GuestListItemDto? GuestPendingDeletion { get; private set; }

    /// <summary>True when the save button may be active — decided by the validation of the domain.</summary>
    public bool CanSave { get; private set; }

    /// <summary>Formularfelt: fornavn.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Formularfelt: efternavn.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Formularfelt: e-mail.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Formularfelt: telefonnummer.</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Formularfelt: land.</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>Form field: passport number. Optional (BR-57).</summary>
    public string? PassportNumber { get; set; }

    /// <summary>
    /// Fetches the first list. Called from <c>OnInitializedAsync</c>.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        SearchAsync(cancellationToken);

    /// <summary>
    /// Searches guests on the current free text.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    public async Task SearchAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;

        try
        {
            var result = await _guests
                .RunAsync((service, token) => service.SearchAsync(SearchText, token), cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                Guests = Array.Empty<GuestListItemDto>();

                return;
            }

            Errors = Array.Empty<string>();
            Guests = result.Value;
        }
        finally
        {
            IsBusy = false;
            IsLoaded = true;
            GuestPendingDeletion = null;
        }
    }

    /// <summary>
    /// Clears the search field and fetches the list again.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    public Task ClearSearchAsync(CancellationToken cancellationToken = default)
    {
        SearchText = null;

        return SearchAsync(cancellationToken);
    }

    /// <summary>
    /// Opens an empty form for a new guest.
    /// </summary>
    public void BeginCreate()
    {
        EditingGuestId = null;

        FirstName = string.Empty;
        LastName = string.Empty;
        Email = string.Empty;
        PhoneNumber = string.Empty;
        Country = string.Empty;
        PassportNumber = null;

        FormErrors = Array.Empty<string>();
        SuccessMessage = null;
        CanSave = false;
        IsFormOpen = true;
    }

    /// <summary>
    /// Opens the form with the fields of an existing guest.
    /// </summary>
    /// <param name="guestId">The id of the guest.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <remarks>
    /// Fetches the guest again rather than using the row of the list: only
    /// <see cref="GuestDetailsDto"/> carries the passport number, because every display of the field is a
    /// dekryptering (B-09, D-04).
    /// </remarks>
    public async Task BeginEditAsync(int guestId, CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        SuccessMessage = null;

        try
        {
            var result = await _guests
                .RunAsync((service, token) => service.GetByIdAsync(guestId, token), cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);

                return;
            }

            var guest = result.Value;

            EditingGuestId = guest.GuestId;
            FirstName = guest.FirstName;
            LastName = guest.LastName;
            Email = guest.Email;
            PhoneNumber = guest.PhoneNumber;
            Country = guest.Country;
            PassportNumber = guest.PassportNumber;

            Errors = Array.Empty<string>();
            FormErrors = Array.Empty<string>();
            IsFormOpen = true;

            Validate();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Closes the form without saving.
    /// </summary>
    public void CancelEdit()
    {
        IsFormOpen = false;
        EditingGuestId = null;
        FormErrors = Array.Empty<string>();
        CanSave = false;
    }

    /// <summary>
    /// Validates the fields of the form against the rules of the domain and updates
    /// <see cref="FormErrors"/> and <see cref="CanSave"/>.
    /// </summary>
    /// <remarks>
    /// Deliberately not async — <see cref="IGuestService.ValidateFields"/> does no IO.
    /// </remarks>
    public void Validate()
    {
        var fields = CurrentFields();
        var result = _guests.Run(service => service.ValidateFields(fields));

        FormErrors = ErrorMessages.Describe(result);
        CanSave = result.IsSuccess;
    }

    /// <summary>
    /// Saves the form — creates a new guest or changes the one being edited.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        var fields = CurrentFields();
        var guestId = EditingGuestId;

        IsBusy = true;

        try
        {
            var result = guestId is null
                ? await _guests
                    .RunAsync(
                        (service, token) => service.CreateAsync(new CreateGuestRequest(fields), token),
                        cancellationToken)
                    .ConfigureAwait(false)
                : await _guests
                    .RunAsync(
                        (service, token) => service.UpdateAsync(new UpdateGuestRequest(guestId.Value, fields), token),
                        cancellationToken)
                    .ConfigureAwait(false);

            if (result.IsFailure)
            {
                FormErrors = ErrorMessages.Describe(result);

                return;
            }

            FormErrors = Array.Empty<string>();
            IsFormOpen = false;
            EditingGuestId = null;
            SuccessMessage = guestId is null
                ? $"{result.Value.FullName} was created."
                : $"{result.Value.FullName} was updated.";
        }
        finally
        {
            IsBusy = false;
        }

        var message = SuccessMessage;

        await SearchAsync(cancellationToken).ConfigureAwait(false);

        SuccessMessage = message;
    }

    /// <summary>
    /// Asks for confirmation before a guest is deleted.
    /// </summary>
    /// <param name="guest">The guest the user wants to delete.</param>
    public void RequestDelete(GuestListItemDto guest)
    {
        ArgumentNullException.ThrowIfNull(guest);

        GuestPendingDeletion = guest;
    }

    /// <summary>
    /// Undoes a deletion that has not yet been confirmed.
    /// </summary>
    public void AbortDelete() => GuestPendingDeletion = null;

    /// <summary>
    /// Deletes the guest the user has confirmed (BR-N-02).
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <remarks>
    /// The service rejects guests with bookings; the screen tries and shows the answer.
    /// </remarks>
    public async Task ConfirmDeleteAsync(CancellationToken cancellationToken = default)
    {
        var guest = GuestPendingDeletion;

        if (guest is null || IsBusy)
        {
            return;
        }

        IsBusy = true;
        SuccessMessage = null;

        try
        {
            var result = await _guests
                .RunAsync((service, token) => service.DeleteAsync(guest.GuestId, token), cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                GuestPendingDeletion = null;

                return;
            }

            Errors = Array.Empty<string>();
            SuccessMessage = $"{guest.FullName} was deleted.";

            if (EditingGuestId == guest.GuestId)
            {
                CancelEdit();
            }
        }
        finally
        {
            IsBusy = false;
        }

        var message = SuccessMessage;

        await SearchAsync(cancellationToken).ConfigureAwait(false);

        SuccessMessage = message;
    }

    /// <summary>
    /// Clears the receipt from the last action.
    /// </summary>
    public void DismissSuccess() => SuccessMessage = null;

    private GuestFields CurrentFields() => new(
        FirstName,
        LastName,
        Email,
        PhoneNumber,
        Country,
        string.IsNullOrWhiteSpace(PassportNumber) ? null : PassportNumber);
}