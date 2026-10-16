using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// En booking med alt hvad detaljeskærmen har brug for.
/// </summary>
/// <param name="BookingId">Bookingens id.</param>
/// <param name="BookingNumber">Vist bookingnummer (BR-109).</param>
/// <param name="StartDate">Ankomstdato.</param>
/// <param name="EndDate">Booket afrejsedato (historik, A-01).</param>
/// <param name="EffectiveEndDate">Den dato rummet faktisk er optaget til (A-01).</param>
/// <param name="Nights">Antal bookede overnatninger.</param>
/// <param name="Status">Bookingens livscyklustilstand.</param>
/// <param name="CheckInTime">Faktisk indtjekningstidspunkt, eller <c>null</c>.</param>
/// <param name="CheckOutTime">Faktisk udtjekningstidspunkt, eller <c>null</c>.</param>
/// <param name="CheckOutDate">Den hotel-lokale dato gæsten tjekkede ud, eller <c>null</c> (A-02, A-03).</param>
/// <param name="Room">Rummet.</param>
/// <param name="Guest">Gæsten, uden pasnummer.</param>
/// <param name="Actions">Hvilke handlinger der er lovlige lige nu (BR-22).</param>
public sealed record BookingDetailsDto(
    int BookingId,
    string BookingNumber,
    DateOnly StartDate,
    DateOnly EndDate,
    DateOnly EffectiveEndDate,
    int Nights,
    BookingStatus Status,
    DateTimeOffset? CheckInTime,
    DateTimeOffset? CheckOutTime,
    DateOnly? CheckOutDate,
    RoomListItemDto Room,
    GuestListItemDto Guest,
    BookingActionsDto Actions);