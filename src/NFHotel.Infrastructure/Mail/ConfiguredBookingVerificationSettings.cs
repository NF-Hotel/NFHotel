using NFHotel.Application.Ports;
using NFHotel.Infrastructure.Config;

namespace NFHotel.Infrastructure.Mail;

/// <summary>
/// The settings of the verification of a public booking (MIL-088, ADR-0052 decision 8) and the fuse of the mails a day
/// (ADR-0053 decision 6), from the sections <c>BookingVerification</c> and <c>Smtp</c> as the start of the API checked
/// them.
/// </summary>
public sealed class ConfiguredBookingVerificationSettings : IBookingVerificationSettings
{
    #region Constructors

    /// <summary>Takes the settings as the start read them.</summary>
    /// <param name="verification">The settings of the verification.</param>
    /// <param name="smtp">The settings of the relay, which hold the fuse.</param>
    /// <exception cref="ArgumentNullException">A section is missing.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The fuse is below one.</exception>
    public ConfiguredBookingVerificationSettings(BookingVerificationOptions verification, SmtpOptions smtp)
    {
        ArgumentNullException.ThrowIfNull(verification);
        ArgumentNullException.ThrowIfNull(smtp);
        ArgumentOutOfRangeException.ThrowIfLessThan(smtp.MaxMailsPerDay, 1);

        Enabled = verification.Enabled;
        SiteAddress = verification.SiteAddress;
        MaxMailsPerDay = smtp.MaxMailsPerDay;
    }

    #endregion Constructors

    #region Properties

    /// <inheritdoc />
    public bool Enabled { get; }

    /// <inheritdoc />
    public Uri? SiteAddress { get; }

    /// <inheritdoc />
    public int MaxMailsPerDay { get; }

    #endregion Properties
}
