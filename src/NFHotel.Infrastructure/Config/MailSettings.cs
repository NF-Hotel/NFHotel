using System.Buffers;
using System.Globalization;

using Microsoft.Extensions.Configuration;

namespace NFHotel.Infrastructure.Config;

/// <summary>
/// Reads and checks the settings of the mail at the start of the API (MIL-088, ADR-0052 decision 8 and ADR-0053
/// decision 2): a wrong value stops the start with the name of its setting, instead of failing at the first guest. The
/// value of <c>Smtp:Password</c> is never part of a message.
/// </summary>
internal static class MailSettings
{
    #region Fields

    private const int MaxPort = 65_535;

    private const int ThumbprintLength = 64;

    private static readonly SearchValues<char> LowerHexDigits = SearchValues.Create("0123456789abcdef");

    #endregion Fields

    #region Public methods

    /// <summary>Reads the section <c>Smtp</c>.</summary>
    /// <param name="configuration">The configuration of the API.</param>
    /// <returns>The settings of the relay.</returns>
    /// <exception cref="InvalidOperationException">A setting has a value that is not allowed; the message names it.</exception>
    public static SmtpOptions ReadSmtp(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        IConfigurationSection section = configuration.GetSection(SmtpOptions.SectionName);
        (string Host, int Port)? forwarder = ReadForwarder(section);

        return new SmtpOptions
        {
            Host = Text(section, nameof(SmtpOptions.Host)),
            Port = ReadPort(section, nameof(SmtpOptions.Port)),
            Security = ReadSecurity(section),
            UserName = Text(section, nameof(SmtpOptions.UserName)),
            Password = Text(section, nameof(SmtpOptions.Password)),
            FromAddress = Text(section, nameof(SmtpOptions.FromAddress)),
            FromName = Text(section, nameof(SmtpOptions.FromName)) ?? SmtpOptions.DefaultFromName,
            ForwarderHost = forwarder?.Host,
            ForwarderPort = forwarder?.Port,
            CertificateThumbprint = ReadThumbprint(section),
            MaxMailsPerDay = ReadAtLeastOne(section, nameof(SmtpOptions.MaxMailsPerDay), SmtpOptions.DefaultMaxMailsPerDay),
        };
    }

    /// <summary>
    /// Reads the section <c>BookingVerification</c>. With the verification on, the site address must be an absolute
    /// https address and the relay must be set; a relay without the verification is allowed, as the state between the
    /// two deploys that switch it on (ADR-0052 decision 8).
    /// </summary>
    /// <param name="configuration">The configuration of the API.</param>
    /// <param name="smtp">The settings of the relay, read before.</param>
    /// <returns>The settings of the verification.</returns>
    /// <exception cref="InvalidOperationException">A setting is wrong or missing; the message names it.</exception>
    public static BookingVerificationOptions ReadVerification(IConfiguration configuration, SmtpOptions smtp)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(smtp);

        IConfigurationSection section = configuration.GetSection(BookingVerificationOptions.SectionName);
        bool enabled = ReadBoolean(section, nameof(BookingVerificationOptions.Enabled));
        Uri? siteAddress = ReadSiteAddress(section, enabled);

        if (enabled)
        {
            RequireForVerification(smtp.Host, nameof(SmtpOptions.Host));
            RequireForVerification(smtp.Port?.ToString(CultureInfo.InvariantCulture), nameof(SmtpOptions.Port));
            RequireForVerification(smtp.UserName, nameof(SmtpOptions.UserName));
            RequireForVerification(smtp.Password, nameof(SmtpOptions.Password));
            RequireForVerification(smtp.FromAddress, nameof(SmtpOptions.FromAddress));
        }

        return new BookingVerificationOptions { Enabled = enabled, SiteAddress = siteAddress };
    }

    #endregion Public methods

    #region Private methods

    private static string? Text(IConfigurationSection section, string key) =>
        string.IsNullOrWhiteSpace(section[key]) ? null : section[key]!.Trim();

    private static string PathOf(IConfigurationSection section, string key) => $"{section.Path}:{key}";

    private static int? ReadPort(IConfigurationSection section, string key) =>
        Text(section, key) is { } value ? ParsePort(value, PathOf(section, key)) : null;

    private static int ParsePort(string value, string path) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int port) && port is >= 1 and <= MaxPort
            ? port
            : throw new InvalidOperationException($"'{path}' must be a port from 1 to {MaxPort}.");

    // ADR-0053 decision 2: only the two encrypted modes; None, a number or any other word stops the start.
    private static SmtpSecurity ReadSecurity(IConfigurationSection section)
    {
        string key = nameof(SmtpOptions.Security);
        return Text(section, key) switch
        {
            null => SmtpSecurity.SslOnConnect,
            { } value when value.Equals(nameof(SmtpSecurity.SslOnConnect), StringComparison.OrdinalIgnoreCase) => SmtpSecurity.SslOnConnect,
            { } value when value.Equals(nameof(SmtpSecurity.StartTls), StringComparison.OrdinalIgnoreCase) => SmtpSecurity.StartTls,
            _ => throw new InvalidOperationException(
                $"'{PathOf(section, key)}' must be {nameof(SmtpSecurity.SslOnConnect)} or {nameof(SmtpSecurity.StartTls)}."),
        };
    }

    // The forwarder of ADR-0053 decision 4 as host:port, for example mail-relay:1465.
    private static (string Host, int Port)? ReadForwarder(IConfigurationSection section)
    {
        string key = "Forwarder";
        if (Text(section, key) is not { } value)
        {
            return null;
        }

        int colon = value.LastIndexOf(':');
        return colon > 0 && colon < value.Length - 1
            ? (value[..colon], ParsePort(value[(colon + 1)..], PathOf(section, key)))
            : throw new InvalidOperationException($"'{PathOf(section, key)}' must be a host and a port, such as mail-relay:1465.");
    }

    private static string? ReadThumbprint(IConfigurationSection section)
    {
        string key = nameof(SmtpOptions.CertificateThumbprint);
        return Text(section, key) switch
        {
            null => null,
            { Length: ThumbprintLength } value when !value.AsSpan().ContainsAnyExcept(LowerHexDigits) => value,
            _ => throw new InvalidOperationException(
                $"'{PathOf(section, key)}' must be the SHA-256 of a certificate as {ThumbprintLength} lower-case hex digits."),
        };
    }

    private static int ReadAtLeastOne(IConfigurationSection section, string key, int fallback)
    {
        if (Text(section, key) is not { } value)
        {
            return fallback;
        }

        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int number) && number >= 1
            ? number
            : throw new InvalidOperationException($"'{PathOf(section, key)}' must be a whole number of at least 1.");
    }

    private static bool ReadBoolean(IConfigurationSection section, string key) =>
        Text(section, key) switch
        {
            null => false,
            { } value when bool.TryParse(value, out bool flag) => flag,
            _ => throw new InvalidOperationException($"'{PathOf(section, key)}' must be true or false."),
        };

    private static Uri? ReadSiteAddress(IConfigurationSection section, bool enabled)
    {
        string key = nameof(BookingVerificationOptions.SiteAddress);
        string path = PathOf(section, key);
        string? value = Text(section, key);
        if (value is null)
        {
            return enabled
                ? throw new InvalidOperationException($"The verification of bookings is on, so '{path}' must be set.")
                : null;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? address))
        {
            throw new InvalidOperationException($"'{path}' must be an absolute address.");
        }

        return !enabled || address.Scheme == Uri.UriSchemeHttps
            ? address
            : throw new InvalidOperationException($"The verification of bookings is on, so '{path}' must be an https address.");
    }

    private static void RequireForVerification(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"The verification of bookings is on, so '{SmtpOptions.SectionName}:{key}' must be set.");
        }
    }

    #endregion Private methods
}
