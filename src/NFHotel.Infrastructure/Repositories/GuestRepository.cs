using Microsoft.EntityFrameworkCore;

using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IGuestRepository"/>.
/// </summary>
/// <remarks>
/// Note what deliberately does not exist: no search and no sorting on the passport number.
/// The field is encrypted with a randomised cipher (B-09), so even an equality lookup would
/// ramme ciffertekst.
/// </remarks>
public sealed class GuestRepository : Repository<Guest, int>, IGuestRepository, IGuestQueries
{
    /// <summary>
    /// Creates the repository.
    /// </summary>
    /// <param name="context">Konteksten.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="context"/> is <c>null</c>.</exception>
    public GuestRepository(HotelDbContext context)
        : base(context)
    {
    }

    // --------------------------------------------------------
    // READING — projections (A-11)
    // --------------------------------------------------------

    /// <inheritdoc />
    public async Task<GuestDetailsDto?> GetDetailsAsync(
        int guestId,
        CancellationToken cancellationToken = default) =>
        await Context.Guests
            .AsNoTracking()
            .Where(guest => guest.Id == guestId)
            .Select(guest => new GuestDetailsDto(
                guest.Id,
                guest.FirstName,
                guest.LastName,
                guest.FirstName + " " + guest.LastName,
                guest.Email,
                guest.PhoneNumber,
                guest.Country,

                // The only place the passport number leaves the database in plain text. EF applies
                // the value converter on the projected column, so the decryption happens here
                // and only here (B-09).
                guest.PassportNumber))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<GuestListItemDto>> SearchAsync(
        IReadOnlyCollection<string> searchTerms,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchTerms);

        var query = Context.Guests.AsNoTracking();

        // BR-68, BR-N-05: AND between words, OR between fields. Each word is applied as its
        // own Where, which is exactly what gives AND. An empty set returns all guests (BR-70).
        foreach (var term in searchTerms)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                continue;
            }

            var pattern = SqlPattern.Contains(term);

            query = query.Where(guest =>
                EF.Functions.ILike(guest.FirstName, pattern, SqlPattern.EscapeCharacter)
                || EF.Functions.ILike(guest.LastName, pattern, SqlPattern.EscapeCharacter)
                || EF.Functions.ILike(guest.Country, pattern, SqlPattern.EscapeCharacter)
                || EF.Functions.ILike(guest.Email, pattern, SqlPattern.EscapeCharacter));
        }

        // The sorting is BR-69 and belongs in the service; here only a deterministic
        // order is ensured, which ix_guest_last_name_first_name can also serve.
        return await query
            .OrderBy(guest => guest.LastName)
            .ThenBy(guest => guest.FirstName)
            .ThenBy(guest => guest.Id)
            .Select(guest => new GuestListItemDto(
                guest.Id,
                guest.FirstName,
                guest.LastName,
                guest.FirstName + " " + guest.LastName,
                guest.Email,
                guest.PhoneNumber,
                guest.Country,

                // Only whether there IS a passport number. IS NOT NULL works fine on ciphertext;
                // the value is not decrypted (B-09).
                guest.PassportNumber != null))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(int guestId, CancellationToken cancellationToken = default) =>
        await Context.Guests
            .AsNoTracking()
            .AnyAsync(guest => guest.Id == guestId, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<bool> HasAnyBookingsAsync(int guestId, CancellationToken cancellationToken = default) =>
        await Context.Bookings
            .AsNoTracking()
            .AnyAsync(booking => booking.GuestId == guestId, cancellationToken)
            .ConfigureAwait(false);

    // --------------------------------------------------------
    // KOMMANDOSTIER — entiteter
    // --------------------------------------------------------

    /// <inheritdoc />
    public void Remove(Guest guest) => MarkForRemoval(guest);
}
