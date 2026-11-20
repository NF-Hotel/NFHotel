namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// The answer to a booking request (MIL-088, ADR-0052 decision 1): the mail is on its way, and its link works until
/// <paramref name="ExpiresAt"/>. It holds neither the token nor the email.
/// </summary>
/// <param name="ExpiresAt">When the link of the mail lapses.</param>
public sealed record BookingRequestReceiptDto(DateTimeOffset ExpiresAt);
