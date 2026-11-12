namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// A guest's lookup of a booking: the booking key and the email that the booking was made with (MIL-079). The email
/// travels in the body of the request, never in an address, so it stays out of access logs.
/// </summary>
/// <param name="BookingId">The booking key: the digits of the booking number (<c>FLZ-000123</c> is 123).</param>
/// <param name="Email">
/// The email of the booking, in any letter case. Nullable on purpose: a missing email reaches the use case, which
/// refuses it with the same code on every path, instead of the model validation of the API answering it alone.
/// </param>
public sealed record BookingReceiptRequest(int BookingId, string? Email);
