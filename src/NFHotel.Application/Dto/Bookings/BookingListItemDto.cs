using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// En booking som den vises i oversigten.
/// </summary>
/// <param name="BookingId">Bookingens id.</param>
/// <param name="BookingNumber">Vist bookingnummer, fx <c>FLZ-000123</c> (BR-109).</param>
/// <param name="StartDate">Ankomstdato.</param>
/// <param name="EndDate">Booket afrejsedato. Historik — ændres aldrig af en udtjekning (A-01).</param>
/// <param name="EffectiveEndDate">
/// Den dato bookingen faktisk lægger beslag på rummet til (A-01). Afviger fra
/// <paramref name="EndDate"/> når gæsten er rejst tidligt.
/// </param>
/// <param name="Nights">Antal bookede overnatninger (BR-109, BR-117).</param>
/// <param name="Status">Bookingens livscyklustilstand.</param>
/// <param name="CheckInTime">Faktisk indtjekningstidspunkt, eller <c>null</c>.</param>
/// <param name="CheckOutTime">Faktisk udtjekningstidspunkt, eller <c>null</c>.</param>
/// <param name="RoomId">Rummets id.</param>
/// <param name="RoomNumber">Rummets værelsesnummer.</param>
/// <param name="GuestId">Gæstens id.</param>
/// <param name="GuestFullName">Gæstens fulde navn (BR-25).</param>
/// <param name="GuestCountry">Gæstens land.</param>
/// <param name="GuestEmail">Gæstens e-mail.</param>
/// <param name="Actions">Hvilke handlinger der er lovlige lige nu (BR-22).</param>
public sealed record BookingListItemDto(
    int BookingId,
    string BookingNumber,
    DateOnly StartDate,
    DateOnly EndDate,
    DateOnly EffectiveEndDate,
    int Nights,
    BookingStatus Status,
    DateTimeOffset? CheckInTime,
    DateTimeOffset? CheckOutTime,
    int RoomId,
    string RoomNumber,
    int GuestId,
    string GuestFullName,
    string GuestCountry,
    string GuestEmail,
    BookingActionsDto Actions);