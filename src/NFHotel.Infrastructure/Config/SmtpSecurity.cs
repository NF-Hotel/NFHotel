namespace NFHotel.Infrastructure.Config;

/// <summary>
/// How the connection to the relay is encrypted (MIL-088, ADR-0053 decision 3). There is no plain text: a setting other
/// than these two stops the start of the API.
/// </summary>
public enum SmtpSecurity
{
    /// <summary>TLS from the first byte, on port 465.</summary>
    SslOnConnect = 0,

    /// <summary>STARTTLS after the greeting, on port 587; a relay that does not offer it is refused.</summary>
    StartTls = 1,
}
