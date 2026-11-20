namespace NFHotel.Infrastructure.Config;

/// <summary>
/// The settings of the relay that the API sends its mails through (MIL-088, ADR-0053 decision 2), read from the section
/// <c>Smtp</c>. <see cref="Password"/> comes from a secret file of the VM and is never logged.
/// </summary>
public sealed class SmtpOptions
{
    #region Fields

    /// <summary>The section of the configuration.</summary>
    public const string SectionName = "Smtp";

    /// <summary>The name that the mails come from when the setting is empty.</summary>
    public const string DefaultFromName = "NF Hotel";

    /// <summary>The fuse of the mails in 24 hours when the setting is empty (ADR-0053 decision 6).</summary>
    public const int DefaultMaxMailsPerDay = 400;

    #endregion Fields

    #region Properties

    /// <summary>The host name of the relay, for the connection and for the check of its certificate.</summary>
    public string? Host { get; init; }

    /// <summary>The port of the relay: 465 with <see cref="SmtpSecurity.SslOnConnect"/>, 587 with <see cref="SmtpSecurity.StartTls"/>.</summary>
    public int? Port { get; init; }

    /// <summary>How the connection to the relay is encrypted.</summary>
    public SmtpSecurity Security { get; init; } = SmtpSecurity.SslOnConnect;

    /// <summary>The account at the relay.</summary>
    public string? UserName { get; init; }

    /// <summary>The password of the account: a secret.</summary>
    public string? Password { get; init; }

    /// <summary>The address that the mails come from.</summary>
    public string? FromAddress { get; init; }

    /// <summary>The name that the mails come from.</summary>
    public string FromName { get; init; } = DefaultFromName;

    /// <summary>The host of the forwarder <c>mail-relay</c> (ADR-0053 decision 4), or <see langword="null"/> when the API reaches the relay itself.</summary>
    public string? ForwarderHost { get; init; }

    /// <summary>The port of the forwarder; set when <see cref="ForwarderHost"/> is.</summary>
    public int? ForwarderPort { get; init; }

    /// <summary>
    /// The SHA-256 of the one certificate to trust instead of the store of the system, 64 lower-case hex digits over its
    /// DER form: for the mail catcher of the stacks only, never in production.
    /// </summary>
    public string? CertificateThumbprint { get; init; }

    /// <summary>The most mails sent in 24 hours before the fuse refuses a new request.</summary>
    public int MaxMailsPerDay { get; init; } = DefaultMaxMailsPerDay;

    #endregion Properties
}
