using NFHotel.Application.Dto.Guests;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Guests;

/// <summary>
/// Præsentationslogik for gæsteoversigten: søgning samt opret, ret og slet gæst.
/// </summary>
/// <remarks>
/// B-10: klassen validerer <b>ingenting</b> selv. Formularen spørger
/// <see cref="IGuestService.ValidateFields"/> ved hver ændring, så BR-52 til BR-56 aldrig
/// findes i Web-laget, og gemknappen aktiveres af domænets svar frem for af en kopi af
/// reglerne. Alle valideringsfejl vises samtidig (BR-60, BR-96).
/// <para>
/// Sletning kræver en bekræftelse først. Servicen afviser en gæst med bookinger (BR-N-02),
/// men en gæst uden bookinger forsvinder for altid — og det skal brugeren nå at sige nej
/// til.
/// </para>
/// </remarks>
public sealed class GuestListViewModel
{
    private readonly IUseCaseRunner<IGuestService> _guests;

    /// <summary>
    /// Opretter viewmodellen.
    /// </summary>
    /// <param name="guests">Kører use cases på <see cref="IGuestService"/>.</param>
    public GuestListViewModel(IUseCaseRunner<IGuestService> guests)
    {
        ArgumentNullException.ThrowIfNull(guests);

        _guests = guests;
    }

    /// <summary>Gæsterne der vises lige nu.</summary>
    public IReadOnlyList<GuestListItemDto> Guests { get; private set; } =
        Array.Empty<GuestListItemDto>();

    /// <summary>Fejl fra listen og fra sletning, allerede oversat til dansk.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    /// <summary>Fejl fra formularen, allerede oversat til dansk.</summary>
    public IReadOnlyList<string> FormErrors { get; private set; } = Array.Empty<string>();

    /// <summary>Kvittering efter et vellykket gem eller en sletning.</summary>
    public string? SuccessMessage { get; private set; }

    /// <summary>Sand mens et kald er i gang.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>Sand når skærmen har hentet data mindst én gang.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>Sand når listen er hentet uden fejl, men er tom.</summary>
    public bool IsEmpty => IsLoaded && Errors.Count == 0 && Guests.Count == 0;

    /// <summary>Sand når den tomme liste skyldes en søgning og ikke et tomt kartotek.</summary>
    public bool IsEmptySearch => IsEmpty && !string.IsNullOrWhiteSpace(SearchText);

    /// <summary>Fritekst på navn, land eller e-mail (BR-68, BR-N-05).</summary>
    public string? SearchText { get; set; }

    /// <summary>Sand når formularen er åben.</summary>
    public bool IsFormOpen { get; private set; }

    /// <summary>Id'et på gæsten der rettes, eller <c>null</c> når en ny oprettes (BR-72).</summary>
    public int? EditingGuestId { get; private set; }

    /// <summary>Formularens overskrift.</summary>
    public string FormTitle => EditingGuestId is null ? "New guest" : "Edit guest";

    /// <summary>Gæsten der venter på at brugeren bekræfter en sletning.</summary>
    public GuestListItemDto? GuestPendingDeletion { get; private set; }

    /// <summary>Sand når gemknappen må være aktiv — afgjort af domænets validering.</summary>
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

    /// <summary>Formularfelt: pasnummer. Valgfrit (BR-57).</summary>
    public string? PassportNumber { get; set; }

    /// <summary>
    /// Henter den første liste. Kaldes fra <c>OnInitializedAsync</c>.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        SearchAsync(cancellationToken);

    /// <summary>
    /// Søger gæster på den nuværende fritekst.
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
    /// Rydder søgefeltet og henter listen igen.
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    public Task ClearSearchAsync(CancellationToken cancellationToken = default)
    {
        SearchText = null;

        return SearchAsync(cancellationToken);
    }

    /// <summary>
    /// Åbner en tom formular til en ny gæst.
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
    /// Åbner formularen med en eksisterende gæsts felter.
    /// </summary>
    /// <param name="guestId">Gæstens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <remarks>
    /// Henter gæsten igen frem for at bruge listens række: kun
    /// <see cref="GuestDetailsDto"/> bærer pasnummeret, fordi hver visning af feltet er en
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
    /// Lukker formularen uden at gemme.
    /// </summary>
    public void CancelEdit()
    {
        IsFormOpen = false;
        EditingGuestId = null;
        FormErrors = Array.Empty<string>();
        CanSave = false;
    }

    /// <summary>
    /// Validerer formularens felter mod domænets regler og opdaterer
    /// <see cref="FormErrors"/> og <see cref="CanSave"/>.
    /// </summary>
    /// <remarks>
    /// Bevidst ikke async — <see cref="IGuestService.ValidateFields"/> laver ingen IO.
    /// </remarks>
    public void Validate()
    {
        var fields = CurrentFields();
        var result = _guests.Run(service => service.ValidateFields(fields));

        FormErrors = ErrorMessages.Describe(result);
        CanSave = result.IsSuccess;
    }

    /// <summary>
    /// Gemmer formularen — opretter en ny gæst eller retter den der redigeres.
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
    /// Beder om bekræftelse før en gæst slettes.
    /// </summary>
    /// <param name="guest">Gæsten brugeren vil slette.</param>
    public void RequestDelete(GuestListItemDto guest)
    {
        ArgumentNullException.ThrowIfNull(guest);

        GuestPendingDeletion = guest;
    }

    /// <summary>
    /// Fortryder en sletning der endnu ikke er bekræftet.
    /// </summary>
    public void AbortDelete() => GuestPendingDeletion = null;

    /// <summary>
    /// Sletter den gæst brugeren har bekræftet (BR-N-02).
    /// </summary>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <remarks>
    /// Servicen afviser gæster med bookinger; skærmen forsøger og viser svaret.
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
    /// Rydder kvitteringen fra sidste handling.
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