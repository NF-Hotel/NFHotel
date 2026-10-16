namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// Flyt en booking til nye datoer og/eller et nyt rum (BR-14, BR-17 til BR-20, BR-108).
/// </summary>
/// <remarks>
/// Hedder bevidst ikke <c>UpdateBookingRequest</c>: use casen er "flyt bookingen", ikke
/// "sæt vilkårlige felter". Status, check-in og check-ud har deres egne use cases.
/// </remarks>
/// <param name="BookingId">Bookingens id.</param>
/// <param name="NewStartDate">Ny ankomstdato. Skal være i dag eller senere (BR-108).</param>
/// <param name="NewEndDate">Ny afrejsedato, eksklusiv.</param>
/// <param name="RoomId">Rummets id — samme som før, eller et nyt rum.</param>
public sealed record RescheduleBookingRequest(
    int BookingId,
    DateOnly NewStartDate,
    DateOnly NewEndDate,
    int RoomId);