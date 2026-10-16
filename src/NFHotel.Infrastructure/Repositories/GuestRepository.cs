using Microsoft.EntityFrameworkCore;

using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Infrastructure.Repositories;

/// <summary>
/// EF Core-implementering af <see cref="IGuestRepository"/>.
/// </summary>
/// <remarks>
/// Bemærk hvad der bevidst ikke findes: ingen søgning og ingen sortering på pasnummer.
/// Feltet er krypteret med en randomiseret ciffer (B-09), så selv et lighedsopslag ville
/// ramme ciffertekst.
/// </remarks>
public sealed class GuestRepository : Repository<Guest, int>, IGuestRepository, IGuestQueries
{
    /// <summary>
    /// Opretter repositoryet.
    /// </summary>
    /// <param name="context">Konteksten.</param>
    /// <exception cref="ArgumentNullException">Kastes hvis <paramref name="context"/> er <c>null</c>.</exception>
    public GuestRepository(HotelDbContext context)
        : base(context)
    {
    }

    // --------------------------------------------------------
    // LÆSNING — projektioner (A-11)
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

                // Eneste sted pasnummeret forlader databasen i klartekst. EF anvender
                // value converteren på den projicerede kolonne, så dekrypteringen sker her
                // og kun her (B-09).
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

        // BR-68, BR-N-05: AND mellem ord, OR mellem felter. Hvert ord lægges på som sit
        // eget Where, hvilket netop giver AND. Et tomt sæt returnerer alle gæster (BR-70).
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

        // Sorteringen er BR-69 og hører i servicen; her sikres kun en deterministisk
        // rækkefølge, som samtidig kan betjenes af ix_guest_last_name_first_name.
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

                // Kun om der ER et pasnummer. IS NOT NULL virker fint på ciffertekst;
                // værdien dekrypteres ikke (B-09).
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