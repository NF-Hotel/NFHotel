namespace NFHotel.Infrastructure.Config;

/// <summary>
/// The settings of the verification of a public booking by email (MIL-088, ADR-0052 decision 8), read from the section
/// <c>BookingVerification</c>. Off by default, so a deploy without a relay keeps the public booking as it was.
/// </summary>
public sealed class BookingVerificationOptions
{
    #region Fields

    /// <summary>The section of the configuration.</summary>
    public const string SectionName = "BookingVerification";

    #endregion Fields

    #region Properties

    /// <summary><see langword="true"/> when a public booking takes a room only after its email is verified.</summary>
    public bool Enabled { get; init; }

    /// <summary>The address of the site that the link of a mail starts with; absolute and https when the verification is on.</summary>
    public Uri? SiteAddress { get; init; }

    #endregion Properties
}
