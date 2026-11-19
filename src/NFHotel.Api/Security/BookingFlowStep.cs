namespace NFHotel.Api.Security;

/// <summary>A step of the booking flow whose outcome is a security event (MIL-087, ADR-0051 decision 6).</summary>
public enum BookingFlowStep
{
    /// <summary>A booking on the public site.</summary>
    PublicBooking,

    /// <summary>A guest sign-in with a reference and an email.</summary>
    GuestSignIn,
}
