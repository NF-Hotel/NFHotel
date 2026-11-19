namespace NFHotel.Application.Ports;

/// <summary>
/// The names of the limits of the booking flow, as the first column of the table "Limits of the booking flow" of
/// <c>docs/refactor/BusinessRules.md</c> and the security events name them (MIL-087, ADR-0051 decisions 1 and 6).
/// </summary>
public static class LimitNames
{
    #region Fields

    /// <summary>The searches for free rooms of one caller address in a minute.</summary>
    public const string Search = "search";

    /// <summary>The public booking requests of one caller address in an hour.</summary>
    public const string BookingHour = "booking_hour";

    /// <summary>The public booking requests of one caller address in a day.</summary>
    public const string BookingDay = "booking_day";

    /// <summary>The coming bookings made on the public site for one email.</summary>
    public const string Email = "email";

    /// <summary>The failed guest sign-ins of one reference or one caller address (ADR-0049 decision 6).</summary>
    public const string GuestSignIn = "guest_sign_in";

    #endregion Fields

    #region Properties

    /// <summary>Every name, in the order of the table.</summary>
    public static IReadOnlyList<string> All { get; } = [Search, BookingHour, BookingDay, Email, GuestSignIn];

    #endregion Properties
}
