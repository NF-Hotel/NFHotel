using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Repositories;

namespace NFHotel.Application.UseCases.Guests;

/// <summary>
/// Implementation of the use cases of guests.
/// </summary>
/// <remarks>
/// All validation goes through <see cref="NFHotel.Domain.Entities.Guest"/>. The service does not re-implement a
/// single rule — it only collects the error codes and packs them in a <see cref="Result"/>.
/// </remarks>
public sealed class GuestService : IGuestService
{
    /// <summary>Characters that separate search words (BR-68). Whitespace, nothing else.</summary>
    private static readonly char[] SearchTermSeparators = [' ', '\t', '\n', '\r'];

    private readonly IGuestRepository _guestRepository;
    private readonly IGuestQueries _guestQueries;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Creates the service.
    /// </summary>
    /// <param name="guestRepository">Data access for guests.</param>
    /// <param name="guestQueries">Reading guests as projections (A-11).</param>
    /// <param name="unitOfWork">The transaction boundary.</param>
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

    /// <summary>Fetches one guest (BR-35, BR-115).</summary>
    /// <param name="guestId">The id of the guest.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The details of the guest, or <see cref="ErrorCodes.Guest.NotFound"/>.</returns>
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

    /// <summary>Searches guests (BR-68, BR-69, BR-70, BR-116, BR-N-05).</summary>
    /// <param name="searchText">Free text. Empty or only whitespace gives all guests (BR-70).</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The matching guests, sorted on "first name last name" in lowercase (BR-69).</returns>
    /// <remarks>
    /// The tokenisation is the <i>rule</i> (BR-68) and therefore belongs here; the matching is data access
    /// and happens in the repository. The sorting is part of the use case and not of the UI — otherwise
    /// every screen would have to know BR-69.
    /// </remarks>
    public async Task<Result<IReadOnlyList<GuestListItemDto>>> SearchAsync(
        string? searchText,
        CancellationToken cancellationToken = default)
    {
        var terms = Tokenize(searchText);

        var guests = await _guestQueries.SearchAsync(terms, cancellationToken).ConfigureAwait(false);

        // BR-69: sorting on the full name in lowercase, so "aa" and "AA" sort the same.
        var sorted = guests
            .OrderBy(static guest => guest.FullName.ToLowerInvariant(), StringComparer.Ordinal)
            .ThenBy(static guest => guest.GuestId)
            .ToArray();

        return Result<IReadOnlyList<GuestListItemDto>>.Success(sorted);
    }

    /// <summary>Creates a guest (BR-52 to BR-57, BR-60, BR-91 to BR-96).</summary>
    /// <param name="request">The fields of the guest.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The created guest, or all validation errors at once.</returns>
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

    /// <summary>Changes a guest (BR-62, BR-66, BR-72, BR-91 to BR-96).</summary>
    /// <param name="request">The new field values plus the id of the guest.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The updated guest, or all validation errors at once.</returns>
    /// <remarks>
    /// The id comes from the request and is never changed (BR-72). If the guest does not exist, it becomes
    /// an error code and not a silent non-update as in the old system.
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

    /// <summary>Deletes a guest (BR-N-02, follows from D-05).</summary>
    /// <param name="guestId">The id of the guest.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>Success, or <see cref="ErrorCodes.Guest.HasBookings"/>.</returns>
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
            // Bookings are history and must not lose their guest. Deleting personal data of a
            // guest with bookings requires an anonymisation strategy (D-05), which is not designed.
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

    /// <summary>Validates the guest fields without saving (BR-58, BR-59).</summary>
    /// <param name="fields">The fields to validate.</param>
    /// <returns>Success, or all broken rules at once.</returns>
    public Result ValidateFields(GuestFields fields) => Validate(fields);

    /// <summary>
    /// Checks guest fields against the rules of the domain. Pure and synchronous: it reads nothing and writes
    /// nothing, so the API client runs it locally instead of calling the API (ADR-0004).
    /// </summary>
    /// <param name="fields">The fields to check.</param>
    /// <returns>A success, or a failure with every error code.</returns>
    public static Result Validate(GuestFields fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        var errors = ValidateInternal(fields);

        return errors.Count > 0
            ? Result.FromCodes(errors)
            : Result.Success();
    }

    // --------------------------------------------------------
    // COMMON
    // --------------------------------------------------------

    /// <summary>
    /// Collects the validation errors of the guest fields by building a guest; the rules are the
    /// attributes of <see cref="NFHotel.Domain.Entities.Guest"/> (ADR-0002). One call, one place — so
    /// <see cref="CreateAsync"/>, <see cref="UpdateAsync"/> and <see cref="ValidateFields"/> cannot validate differently.
    /// </summary>
    /// <param name="fields">Felterne.</param>
    /// <returns>Error codes in field order. An empty list means valid.</returns>
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
    /// Splits the search text into words (BR-68). Empty or only whitespace gives an empty set, which
    /// the repository interprets as "all guests" (BR-70).
    /// </summary>
    /// <param name="searchText">Free text from the user.</param>
    /// <returns>The search words without empty elements.</returns>
    private static IReadOnlyCollection<string> Tokenize(string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return Array.Empty<string>();
        }

        return searchText.Split(SearchTermSeparators, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Saves and builds the answer. Collects the translation of infrastructure errors in one place.
    /// </summary>
    /// <param name="guest">The guest that has just been changed.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The details of the guest, or an error code.</returns>
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
