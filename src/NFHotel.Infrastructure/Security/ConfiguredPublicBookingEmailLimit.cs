using NFHotel.Application.Ports;
using NFHotel.Infrastructure.Config;

namespace NFHotel.Infrastructure.Security;

/// <summary>
/// The limit per email of the public booking, read from the settings <c>SenderLimits</c> (MIL-087, ADR-0051 decision 3):
/// the use case compares it with the count of the database.
/// </summary>
public sealed class ConfiguredPublicBookingEmailLimit : IPublicBookingEmailLimit
{
    #region Constructors

    /// <summary>Reads the number from the settings.</summary>
    /// <param name="options">The settings of the limits of the booking flow.</param>
    /// <exception cref="ArgumentOutOfRangeException">The number is below one.</exception>
    public ConfiguredPublicBookingEmailLimit(SenderLimitOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.ComingPublicBookingsPerEmail, 1);

        ComingPublicBookingsPerEmail = options.ComingPublicBookingsPerEmail;
    }

    #endregion Constructors

    #region Properties

    /// <inheritdoc />
    public int ComingPublicBookingsPerEmail { get; }

    #endregion Properties
}
