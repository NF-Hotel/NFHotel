using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;

using MimeKit;
using MimeKit.Text;

using NFHotel.Application.Ports;
using NFHotel.Infrastructure.Config;

namespace NFHotel.Infrastructure.Mail;

/// <summary>
/// Sends the mails of the booking flow to the relay with SMTP over TLS, through MailKit (MIL-088, ADR-0053 decisions 1
/// to 5): TLS from the first byte or STARTTLS and never plain text; the certificate of the relay checked against the
/// store of the system and its host name, or, in the stacks only, against the one pinned certificate of the mail
/// catcher; through the forwarder <c>mail-relay</c> when it is set, with TLS run for the host name of the relay.
/// </summary>
/// <remarks>
/// The revocation of the certificate of the relay is not checked: the API reaches nothing outside the VM but the relay,
/// through the forwarder, so it cannot fetch a revocation list, and an online check would refuse every mail. The chain,
/// the dates and the host name are checked. A relay that is down, refuses the mail or does not answer within
/// <see cref="SendTimeout"/> is a result with the reason in a word, never an exception and never a text of the relay. A
/// send that the caller cancels ends in <see cref="OperationCanceledException"/>, at whatever step it was.
/// </remarks>
public sealed class SmtpMailSender : IMailSender
{
    #region Fields

    private readonly SmtpOptions _options;
    private readonly TimeSpan _sendTimeout;

    #endregion Fields

    #region Constructors

    /// <summary>Takes the settings of the relay as the start read them, with the bound <see cref="SendTimeout"/>.</summary>
    /// <param name="options">The settings of the relay.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public SmtpMailSender(SmtpOptions options)
        : this(options, SendTimeout)
    {
    }

    /// <summary>Takes the settings of the relay and another bound of a send: for the tests, which cannot wait that long.</summary>
    /// <param name="options">The settings of the relay.</param>
    /// <param name="sendTimeout">The most time that one send may take, from the connection to the end of the session.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sendTimeout"/> is not positive.</exception>
    public SmtpMailSender(SmtpOptions options, TimeSpan sendTimeout)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sendTimeout, TimeSpan.Zero);

        _options = options;
        _sendTimeout = sendTimeout;
    }

    #endregion Constructors

    #region Properties

    /// <summary>
    /// The most time that one send may take, from the connection to the end of the session with the relay, the quit
    /// included (ADR-0053 decision 1): well below the 30 seconds that the web application waits for the API, so a slow
    /// relay ends in a result of the send, never in an error page after the booking was made (RC-436 F3).
    /// </summary>
    public static TimeSpan SendTimeout { get; } = TimeSpan.FromSeconds(15);

    #endregion Properties

    #region Public methods

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">A setting that a send needs is missing; the start of the API refuses that when the verification is on.</exception>
    public async Task<MailSendResult> SendAsync(OutgoingMail mail, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mail);
        GuardOneLine(mail.To, nameof(mail));
        GuardOneLine(mail.Subject, nameof(mail));

        MimeMessage message;
        try
        {
            message = Build(mail);
        }
        catch (ParseException)
        {
            return MailSendResult.Failed(MailFailureReasons.Refused);
        }

        string userName = Required(_options.UserName, nameof(SmtpOptions.UserName));
        string password = Required(_options.Password, nameof(SmtpOptions.Password));

        // One bound for the whole session, the quit included; the quit is not the caller's to cancel.
        using CancellationTokenSource deadline = new(_sendTimeout);
        using CancellationTokenSource limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        using SmtpClient client = NewClient();
        bool connected = false;
        try
        {
            await ConnectAsync(client, limit.Token).ConfigureAwait(false);
            connected = true;
            await client.AuthenticateAsync(userName, password, limit.Token).ConfigureAwait(false);
            await client.SendAsync(message, limit.Token).ConfigureAwait(false);
        }
        catch (Exception exception) when (cancellationToken.IsCancellationRequested && exception is not OperationCanceledException)
        {
            // MailKit wraps a cancellation in the TLS handshake in SslHandshakeException; the caller gets its cancellation.
            throw new OperationCanceledException("The caller cancelled the send.", exception, cancellationToken);
        }
        catch (Exception exception) when (ReasonOf(exception, connected, cancellationToken, deadline.Token) is { } reason)
        {
            return MailSendResult.Failed(reason);
        }

        // The relay has taken the mail: a quit that fails or runs out of the bound changes nothing for it.
        await QuitQuietlyAsync(client, deadline.Token).ConfigureAwait(false);
        return MailSendResult.Success;
    }

    #endregion Public methods

    #region Private methods

    // ASVS 1.3.11: a line break in the address or the subject could add a header, so the mail is refused before it is built.
    private static void GuardOneLine(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrEmpty(value, parameterName);
        if (value.AsSpan().ContainsAny('\r', '\n'))
        {
            throw new ArgumentException("The address and the subject of a mail must be one line.", parameterName);
        }
    }

    // Maps a failure of a send to its reason in a word; null for a failure that is no result of the relay, which goes on
    // up: a bug, or a cancellation by the caller. Once the bound of the send has run out, a failure is a timeout at
    // whatever step it came, the TLS handshake included, where MailKit wraps it in SslHandshakeException.
    private static string? ReasonOf(Exception exception, bool connected, CancellationToken callerToken, CancellationToken deadline) =>
        exception switch
        {
            OperationCanceledException when callerToken.IsCancellationRequested => null,
            OperationCanceledException or TimeoutException => MailFailureReasons.Timeout,
            _ when deadline.IsCancellationRequested => MailFailureReasons.Timeout,
            SslHandshakeException { InnerException: OperationCanceledException or TimeoutException } => MailFailureReasons.Timeout,
            MailKit.Security.AuthenticationException or SaslException => MailFailureReasons.Authentication,
            SslHandshakeException or System.Security.Authentication.AuthenticationException => MailFailureReasons.Tls,

            // MailKit refuses a relay that does not offer STARTTLS with NotSupportedException while it connects; later the
            // same type means that the relay lacks something that the mail needs.
            NotSupportedException => connected ? MailFailureReasons.Refused : MailFailureReasons.Tls,
            SmtpCommandException => MailFailureReasons.Refused,
            SocketException => MailFailureReasons.Connect,
            IOException => connected ? MailFailureReasons.Other : MailFailureReasons.Connect,
            ProtocolException or ServiceNotConnectedException or ServiceNotAuthenticatedException => MailFailureReasons.Other,
            _ => null,
        };

    // The quit has what is left of the bound of the send.
    private static async Task QuitQuietlyAsync(SmtpClient client, CancellationToken deadline)
    {
        try
        {
            await client.DisconnectAsync(quit: true, deadline).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or SocketException or ProtocolException
            or OperationCanceledException or TimeoutException)
        {
            // The mail is with the relay; the connection closes when the client is disposed.
        }
    }

    // The pin of the mail catcher of the stacks (ADR-0053 decision 7): the SHA-256 of the DER form of its certificate.
    private static bool IsPinned(X509Certificate? certificate, string thumbprint) =>
        certificate is not null
        && string.Equals(Convert.ToHexStringLower(SHA256.HashData(certificate.GetRawCertData())), thumbprint, StringComparison.Ordinal);

    private SmtpClient NewClient()
    {
        // The time limit of MailKit for one step is only a backstop, twice the bound of the send, so the bound always ends
        // a send first and reads as a timeout.
        SmtpClient client = new()
        {
            Timeout = (int)(_sendTimeout * 2).TotalMilliseconds,
            CheckCertificateRevocation = false,
        };

        if (_options.CertificateThumbprint is { Length: > 0 } thumbprint)
        {
            client.ServerCertificateValidationCallback = (_, certificate, _, _) => IsPinned(certificate, thumbprint);
        }

        return client;
    }

    private MimeMessage Build(OutgoingMail mail)
    {
        string fromAddress = Required(_options.FromAddress, nameof(SmtpOptions.FromAddress));

        TextPart body = new(TextFormat.Plain);
        body.SetText(Encoding.UTF8, mail.Body);

        MimeMessage message = new() { Subject = mail.Subject, Body = body };
        message.From.Add(new MailboxAddress(_options.FromName, fromAddress));
        message.To.Add(MailboxAddress.Parse(mail.To));
        return message;
    }

    private async Task ConnectAsync(SmtpClient client, CancellationToken cancellationToken)
    {
        string host = Required(_options.Host, nameof(SmtpOptions.Host));
        int port = _options.Port ?? throw Missing(nameof(SmtpOptions.Port));
        SecureSocketOptions security = _options.Security == SmtpSecurity.StartTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.SslOnConnect;

        if (_options.ForwarderHost is not { Length: > 0 } forwarderHost)
        {
            await client.ConnectAsync(host, port, security, cancellationToken).ConfigureAwait(false);
            return;
        }

        // The forwarder passes the bytes on to the relay; TLS runs with the host name of the relay, so the certificate is
        // checked as without the forwarder (ADR-0053 decision 4). The client owns the socket once it is connected.
        int forwarderPort = _options.ForwarderPort ?? throw Missing("Forwarder");
        Socket socket = new(SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync(forwarderHost, forwarderPort, cancellationToken).ConfigureAwait(false);
            await client.ConnectAsync(socket, host, port, security, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private static string Required(string? value, string name) =>
        value is { Length: > 0 } ? value : throw Missing(name);

    private static InvalidOperationException Missing(string name) =>
        new($"The setting '{SmtpOptions.SectionName}:{name}' is missing, so no mail can be sent.");

    #endregion Private methods
}
