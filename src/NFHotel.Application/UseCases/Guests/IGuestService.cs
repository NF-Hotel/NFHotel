using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;

namespace NFHotel.Application.UseCases.Guests;

/// <summary>
/// Use cases for gæster.
/// </summary>
public interface IGuestService
{
    /// <summary>
    /// Henter én gæst (BR-35, BR-115).
    /// </summary>
    /// <param name="guestId">Gæstens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Gæstens detaljer, eller <see cref="ErrorCodes.Guest.NotFound"/>.</returns>
    /// <remarks>Bruges også til at forudfylde en ny booking for en kendt gæst.</remarks>
    Task<Result<GuestDetailsDto>> GetByIdAsync(int guestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Søger gæster (BR-68, BR-69, BR-70, BR-116, BR-N-05).
    /// </summary>
    /// <param name="searchText">
    /// Fritekst. Deles i ord; hvert ord skal findes i fornavn, efternavn, land eller e-mail.
    /// Tom eller kun whitespace giver alle gæster (BR-70).
    /// </param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>De matchende gæster, sorteret på "fornavn efternavn" i lowercase (BR-69).</returns>
    Task<Result<IReadOnlyList<GuestListItemDto>>> SearchAsync(
        string? searchText,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opretter en gæst (BR-52 til BR-57, BR-60, BR-91 til BR-96).
    /// </summary>
    /// <param name="request">Gæstens felter.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Den oprettede gæst, eller alle valideringsfejl på én gang (BR-60, BR-96).</returns>
    Task<Result<GuestDetailsDto>> CreateAsync(
        CreateGuestRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retter en gæst (BR-62, BR-66, BR-72, BR-91 til BR-96).
    /// </summary>
    /// <param name="request">De nye feltværdier plus gæstens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Den opdaterede gæst, eller alle valideringsfejl på én gang.</returns>
    Task<Result<GuestDetailsDto>> UpdateAsync(
        UpdateGuestRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sletter en gæst (BR-N-02, følger af D-05).
    /// </summary>
    /// <param name="guestId">Gæstens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Succes, eller <see cref="ErrorCodes.Guest.HasBookings"/> hvis gæsten har bookinger.</returns>
    /// <remarks>
    /// Reglen findes ikke blandt de 126 porterede regler og er registreret som ny (BR-N-02).
    /// </remarks>
    Task<Result> DeleteAsync(int guestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validerer gæstefelterne uden at gemme (BR-58, BR-59).
    /// </summary>
    /// <param name="fields">Felterne der skal valideres.</param>
    /// <returns>Succes, eller alle brudte regler på én gang.</returns>
    /// <remarks>
    /// <b>Bevidst ikke async</b> — der er ingen IO, og en metode uden reelt async arbejde
    /// skal ikke være async bare for syns skyld.
    /// <para>
    /// Metoden lader UI'et give feedback pr. tastetryk og aktivere gemknappen uden at
    /// duplikere en eneste valideringsregel. Det er den der gør at BR-52 til BR-56 aldrig
    /// behøver eksistere i Web-laget.
    /// </para>
    /// </remarks>
    Result ValidateFields(GuestFields fields);
}