namespace NFHotel.Api.Security;

/// <summary>What a limit per caller address of the booking flow counts (MIL-087, ADR-0051 decision 2).</summary>
public enum SenderLimitKind
{
    /// <summary>A search for free rooms.</summary>
    Search,

    /// <summary>A booking request on the public site.</summary>
    BookingRequest,
}
