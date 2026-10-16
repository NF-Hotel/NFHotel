using NFHotel.Application.Dto.Guests;

namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// Opret en booking (BR-01, BR-41 til BR-49, BR-51, BR-101 til BR-106).
/// </summary>
/// <remarks>
/// Bærer BR-47: <b>præcis én</b> af <see cref="GuestId"/> og <see cref="NewGuest"/> skal
/// være udfyldt. At begge eller ingen er sat er en valideringsfejl fra servicen — ikke en
/// <c>NullReferenceException</c> nede i en ViewModel.
/// </remarks>
/// <param name="CheckInDate">Ankomstdato, inklusiv.</param>
/// <param name="CheckOutDate">Afrejsedato, eksklusiv. Skal være efter ankomstdatoen (BR-103).</param>
/// <param name="RoomId">Rummets id (BR-45, BR-105).</param>
/// <param name="GuestId">En eksisterende gæsts id, eller <c>null</c> hvis gæsten oprettes nu.</param>
/// <param name="NewGuest">Felterne til en ny gæst, eller <c>null</c> hvis en eksisterende er valgt.</param>
public sealed record CreateBookingRequest(
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int RoomId,
    int? GuestId,
    GuestFields? NewGuest);