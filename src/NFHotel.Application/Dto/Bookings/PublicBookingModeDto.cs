namespace NFHotel.Application.Dto.Bookings;

/// <summary>Which way a booking on the public site goes (MIL-088, ADR-0052 decision 8).</summary>
/// <param name="VerificationRequired">
/// <see langword="true"/> when the guest asks and verifies by email; <see langword="false"/> when the booking is made at once.
/// </param>
public sealed record PublicBookingModeDto(bool VerificationRequired);
