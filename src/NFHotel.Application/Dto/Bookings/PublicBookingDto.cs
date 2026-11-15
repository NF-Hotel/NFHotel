using NFHotel.Application.Dto.Guests;

namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// The answer to a booking from the public page (MIL-018, ADR-0047): the new booking and the guest session that signs
/// its guest in, so the guest who has just booked can manage the booking without signing in.
/// </summary>
/// <param name="Booking">The booking that was created. Its guest carries no passport number.</param>
/// <param name="Session">The guest token for the new booking, with the same form and lifetime as a guest sign-in.</param>
public sealed record PublicBookingDto(BookingDetailsDto Booking, GuestSessionDto Session);
