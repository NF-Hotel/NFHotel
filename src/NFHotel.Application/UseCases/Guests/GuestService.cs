using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Repositories;

namespace NFHotel.Application.UseCases.Guests;

/// <summary>
/// Implementering af gæsternes use cases.
/// </summary>
/// <remarks>
/// Al validering går gennem <see cref="NFHotel.Domain.Entities.Guest"/>. Servicen genimplementerer ikke en
/// eneste regel — den samler kun fejlkoderne op og pakker dem i et <see cref="Result"/>.
/// </remarks>
public sealed class GuestService : IGuestService
{
    /// <summary>Tegn der adskiller søgeord (BR-68). Whitespace, intet andet.</summary>
    private static readonly char[] SearchTermSeparators = [' ', '\t', '\n', '\r'];

    private readonly IGuestRepository _guestRepository;
    private readonly IGuestQueries _guestQueries;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Opretter servicen.
    /// </summary>
    /// <param name="guestRepository">Dataadgang for gæster.</param>
    /// <param name="guestQueries">Læsning af gæster som projektioner (A-11).</param>
    /// <param name="unitOfWork">Transaktionsgrænsen.</param>
    public GuestService(
        IGuestRepository guestRepository,
        IGuestQueries guestQueries,
        IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(guestRepository);
        ArgumentNullException.ThrowIfNull(guestQueries);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _guestRepository = guestRepository;
        _guestQueries = guestQueries;
        _unitOfWork = unitOfWork;
    }

    /// <summary>Henter én gæst (BR-35, BR-115).</summary>
    /// <param name="guestId">Gæstens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Gæstens detaljer, eller <see cref="ErrorCodes.Guest.NotFound"/>.</returns>
    public async Task<Result<GuestDetailsDto>> GetByIdAsync(
        int guestId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(guestId);

        var details = await _guestQueries.GetDetailsAsync(guestId, cancellationToken).ConfigureAwait(false);

        return details is null
            ? Result<GuestDetailsDto>.Failure(ErrorCodes.Guest.NotFound)
            : Result<GuestDetailsDto>.Success(details);
    }

    /// <summary>Søger gæster (BR-68, BR-69, BR-70, BR-116, BR-N-05).</summary>
    /// <param name="searchText">Fritekst. Tom eller kun whitespace giver alle gæster (BR-70).</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>De matchende gæster, sorteret på "fornavn efternavn" i lowercase (BR-69).</returns>
    /// <remarks>
    /// Tokeniseringen er <i>reglen</i> (BR-68) og hører derfor her; matchningen er dataadgang
    /// og sker i repositoryet. Sorteringen er en del af use casen og ikke af UI'et — ellers
    /// ville hver skærm skulle kende BR-69.
    /// </remarks>
    public async Task<Result<IReadOnlyList<GuestListItemDto>>> SearchAsync(
        string? searchText,
        CancellationToken cancellationToken = default)
    {
        var terms = Tokenize(searchText);

        var guests = await _guestQueries.SearchAsync(terms, cancellationToken).ConfigureAwait(false);

        // BR-69: sortering på det fulde navn i lowercase, så "aa" og "AA" sorteres ens.
        var sorted = guests
            .OrderBy(static guest => guest.FullName.ToLowerInvariant(), StringComparer.Ordinal)
            .ThenBy(static guest => guest.GuestId)
            .ToArray();

        return Result<IReadOnlyList<GuestListItemDto>>.Success(sorted);
    }

    /// <summary>Opretter en gæst (BR-52 til BR-57, BR-60, BR-91 til BR-96).</summary>
    /// <param name="request">Gæstens felter.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Den oprettede gæst, eller alle valideringsfejl på én gang.</returns>
    public async Task<Result<GuestDetailsDto>> CreateAsync(
        CreateGuestRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Fields);

        var fields = request.Fields;
        var errors = ValidateInternal(fields);

        if (errors.Count > 0)
        {
            return Result<GuestDetailsDto>.FromCodes(errors);
        }

        Guest guest;

        try
        {
            guest = Guest.Create(
                fields.FirstName,
                fields.LastName,
                fields.Email,
                fields.PhoneNumber,
                fields.Country,
                fields.PassportNumber);
        }
        catch (DomainException exception)
        {
            return Result<GuestDetailsDto>.Failure(exception.Code);
        }

        _guestRepository.Add(guest);

        return await SaveAndDescribeAsync(guest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Retter en gæst (BR-62, BR-66, BR-72, BR-91 til BR-96).</summary>
    /// <param name="request">De nye feltværdier plus gæstens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Den opdaterede gæst, eller alle valideringsfejl på én gang.</returns>
    /// <remarks>
    /// Id'et kommer fra requesten og ændres aldrig (BR-72). Findes gæsten ikke, bliver det
    /// en fejlkode og ikke en lydløs ikke-opdatering som i det gamle system.
    /// </remarks>
    public async Task<Result<GuestDetailsDto>> UpdateAsync(
        UpdateGuestRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Fields);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.GuestId);

        var fields = request.Fields;
        var errors = ValidateInternal(fields);

        if (errors.Count > 0)
        {
            return Result<GuestDetailsDto>.FromCodes(errors);
        }

        var guest = await _guestRepository.GetByIdAsync(request.GuestId, cancellationToken).ConfigureAwait(false);

        if (guest is null)
        {
            return Result<GuestDetailsDto>.Failure(ErrorCodes.Guest.NotFound);
        }

        try
        {
            guest.UpdateDetails(
                fields.FirstName,
                fields.LastName,
                fields.Email,
                fields.PhoneNumber,
                fields.Country,
                fields.PassportNumber);
        }
        catch (DomainException exception)
        {
            return Result<GuestDetailsDto>.Failure(exception.Code);
        }

        _guestRepository.Update(guest);

        return await SaveAndDescribeAsync(guest, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sletter en gæst (BR-N-02, følger af D-05).</summary>
    /// <param name="guestId">Gæstens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Succes, eller <see cref="ErrorCodes.Guest.HasBookings"/>.</returns>
    public async Task<Result> DeleteAsync(int guestId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(guestId);

        var guest = await _guestRepository.GetByIdAsync(guestId, cancellationToken).ConfigureAwait(false);

        if (guest is null)
        {
            return Result.Failure(ErrorCodes.Guest.NotFound);
        }

        var hasBookings = await _guestRepository.HasAnyBookingsAsync(guestId, cancellationToken).ConfigureAwait(false);

        if (hasBookings)
        {
            // Bookinger er historik og må ikke miste sin gæst. Sletning af persondata på en
            // gæst med bookinger kræver en anonymiseringsstrategi (D-05), som ikke er designet.
            return Result.Failure(ErrorCodes.Guest.HasBookings);
        }

        _guestRepository.Remove(guest);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure(ErrorCodes.Guest.ConcurrencyConflict);
        }

        return Result.Success();
    }

    /// <summary>Validerer gæstefelterne uden at gemme (BR-58, BR-59).</summary>
    /// <param name="fields">Felterne der skal valideres.</param>
    /// <returns>Succes, eller alle brudte regler på én gang.</returns>
    public Result ValidateFields(GuestFields fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        var errors = ValidateInternal(fields);

        return errors.Count > 0
            ? Result.FromCodes(errors)
            : Result.Success();
    }

    // --------------------------------------------------------
    // FÆLLES
    // --------------------------------------------------------

    /// <summary>
    /// Kører domænets validering på felterne. Ét kald, ét sted — så
    /// <see cref="CreateAsync"/>, <see cref="UpdateAsync"/> og <see cref="ValidateFields"/>
    /// ikke kan komme til at validere forskelligt.
    /// </summary>
    /// <param name="fields">Felterne.</param>
    /// <returns>Fejlkoder i feltrækkefølge. Tom liste betyder gyldig.</returns>
    internal static IReadOnlyList<string> ValidateInternal(GuestFields fields)
    {
        try
        {
            _ = Guest.Create(
                fields.FirstName,
                fields.LastName,
                fields.Email,
                fields.PhoneNumber,
                fields.Country,
                fields.PassportNumber);

            return [];
        }
        catch (DomainException exception)
        {
            return exception.Codes;
        }
    }

    /// <summary>
    /// Deler søgeteksten i ord (BR-68). Tom eller kun whitespace giver et tomt sæt, som
    /// repositoryet fortolker som "alle gæster" (BR-70).
    /// </summary>
    /// <param name="searchText">Fritekst fra brugeren.</param>
    /// <returns>Søgeordene uden tomme elementer.</returns>
    private static IReadOnlyCollection<string> Tokenize(string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return Array.Empty<string>();
        }

        return searchText.Split(SearchTermSeparators, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Gemmer og bygger svaret. Samler oversættelsen af infrastrukturfejl ét sted.
    /// </summary>
    /// <param name="guest">Gæsten der netop er ændret.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Gæstens detaljer, eller en fejlkode.</returns>
    private async Task<Result<GuestDetailsDto>> SaveAndDescribeAsync(
        Guest guest,
        CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (ConcurrencyConflictException)
        {
            return Result<GuestDetailsDto>.Failure(ErrorCodes.Guest.ConcurrencyConflict);
        }

        return Result<GuestDetailsDto>.Success(guest.ToDetails());
    }
}