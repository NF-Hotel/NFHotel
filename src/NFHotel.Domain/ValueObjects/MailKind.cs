namespace NFHotel.Domain.ValueObjects;

/// <summary>The kind of a mail that the application sends (MIL-088, ADR-0052 decisions 4 and 5).</summary>
/// <remarks>Stored as its number in the record of a mail sent; the numbers never change.</remarks>
public enum MailKind
{
    /// <summary>The mail of a booking request, with the link that verifies it.</summary>
    VerificationLink = 0,

    /// <summary>The mail after a verification, with the reference and the dates of the booking.</summary>
    BookingReceipt = 1,
}
