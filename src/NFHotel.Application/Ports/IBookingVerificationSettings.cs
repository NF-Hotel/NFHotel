namespace NFHotel.Application.Ports;

/// <summary>
/// The settings of the verification of a public booking by email (MIL-088, ADR-0052 decision 8) and the fuse of the mails
/// a day (ADR-0053 decision 6). The infrastructure reads them at the start and refuses a start with the verification on
/// and a setting of the relay or the site address missing.
/// </summary>
public interface IBookingVerificationSettings
{
    #region Properties

    /// <summary>
    /// <see langword="true"/> when a public booking takes a room only after its email is verified; then the booking at
    /// once is refused. Off by default.
    /// </summary>
    bool Enabled { get; }

    /// <summary>The address of the site that the link of a mail starts with: absolute and https when the verification is on.</summary>
    Uri? SiteAddress { get; }

    /// <summary>The most mails sent in 24 hours before the fuse refuses a new request (400 by default).</summary>
    int MaxMailsPerDay { get; }

    #endregion Properties
}
