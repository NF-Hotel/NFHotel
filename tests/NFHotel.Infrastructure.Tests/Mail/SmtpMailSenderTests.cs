using System.Net;
using System.Net.Sockets;

using NFHotel.Application.Ports;
using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Mail;

namespace NFHotel.Infrastructure.Tests.Mail;

/// <summary>
/// The adapter that sends the mails to the relay (MIL-088 criterion 6, ADR-0053 decisions 1 to 5 and 7), against a relay
/// of the test on the loopback address: TLS from the first byte and after STARTTLS, the pinned certificate, the
/// forwarder, and each failure as a reason in a word, never an exception and never a text of the relay.
/// </summary>
public sealed class SmtpMailSenderTests
{
    private const string UserName = "relay-user@example.com";
    private const string Password = "relay-password-of-the-test";
    private const string FromAddress = "noreply@example.com";

    private static readonly OutgoingMail Mail = new("anna@example.com", "Verify your booking at NF Hotel", "Press the button on the page of the link.");

    // The bound of a send in the tests that wait for it to run out, instead of the 15 seconds of production.
    private static readonly TimeSpan ShortBound = TimeSpan.FromSeconds(3);

    #region Sent

    /// <summary>
    /// Positive (ADR-0053 decisions 1 and 7): with TLS from the first byte and the pinned certificate the relay takes the
    /// mail, from the from address of the settings to the address of the mail, after a sign-in with the user of the
    /// settings.
    /// </summary>
    [Fact]
    public async Task SendAsync_RelayWithTlsFromTheFirstByte_TakesTheMail()
    {
        // Arrange
        await using FakeSmtpRelay relay = new();
        SmtpMailSender sender = new(Options(relay));

        // Act
        MailSendResult result = await sender.SendAsync(Mail, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(MailSendResult.Success, result);
        Assert.Equal([UserName], relay.UserNames);
        Assert.Contains($"MAIL FROM:<{FromAddress}>", relay.Commands);
        Assert.Contains("RCPT TO:<anna@example.com>", relay.Commands);
        string mail = Assert.Single(relay.Mails);
        Assert.Contains("Subject: Verify your booking at NF Hotel", mail, StringComparison.Ordinal);
        Assert.Contains($"NF Hotel <{FromAddress}>", mail, StringComparison.Ordinal);
        Assert.Contains("Content-Type: text/plain", mail, StringComparison.Ordinal);
    }

    /// <summary>Positive (ADR-0053 decision 2): with STARTTLS the connection is encrypted before the sign-in, and the relay takes the mail.</summary>
    [Fact]
    public async Task SendAsync_RelayWithStartTls_TakesTheMailOverTls()
    {
        // Arrange
        await using FakeSmtpRelay relay = new(implicitTls: false);
        SmtpMailSender sender = new(Options(relay, SmtpSecurity.StartTls));

        // Act
        MailSendResult result = await sender.SendAsync(Mail, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(MailSendResult.Success, result);
        Assert.Contains("STARTTLS", relay.Commands);
        Assert.Single(relay.TlsHostNames);
        Assert.Single(relay.Mails);
    }

    /// <summary>
    /// Positive (ADR-0053 decision 4): through the forwarder the connection goes to the forwarder, and TLS runs for the
    /// host name of the relay, which here cannot even be resolved.
    /// </summary>
    [Fact]
    public async Task SendAsync_ThroughTheForwarder_RunsTlsForTheHostOfTheRelay()
    {
        // Arrange
        await using FakeSmtpRelay relay = new();
        SmtpMailSender sender = new(Options(
            relay,
            host: "smtp.relay.invalid",
            port: 465,
            forwarderHost: IPAddress.Loopback.ToString(),
            forwarderPort: relay.Port));

        // Act
        MailSendResult result = await sender.SendAsync(Mail, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(MailSendResult.Success, result);
        Assert.Equal(["smtp.relay.invalid"], relay.TlsHostNames);
    }

    /// <summary>
    /// Positive (ADR-0053 decision 1, RC-436 F3): a relay that takes the mail and never answers the quit holds the send no
    /// longer than its one bound, the quit included, and the mail counts as sent. The test fails at its own time limit
    /// when the quit waits beyond the bound.
    /// </summary>
    [Fact(Timeout = 20_000)]
    public async Task SendAsync_RelayNeverAnswersTheQuit_IsSentWithinTheBound()
    {
        // Arrange
        await using FakeSmtpRelay relay = new() { SilentAtTheQuit = true };
        SmtpMailSender sender = new(Options(relay), ShortBound);

        // Act
        MailSendResult result = await sender.SendAsync(Mail, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(MailSendResult.Success, result);
        Assert.Single(relay.Mails);
    }

    #endregion

    #region Refused

    /// <summary>
    /// Security (criterion 6, ADR-0053 decision 2): a relay that offers no STARTTLS is refused before any password is
    /// sent, as a failure of TLS.
    /// </summary>
    [Fact]
    public async Task SendAsync_RelayWithoutStartTls_IsAFailureOfTlsBeforeTheSignIn()
    {
        // Arrange
        await using FakeSmtpRelay relay = new(implicitTls: false) { OffersStartTls = false };
        SmtpMailSender sender = new(Options(relay, SmtpSecurity.StartTls));

        // Act
        MailSendResult result = await sender.SendAsync(Mail, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(MailSendResult.Failed(MailFailureReasons.Tls), result);
        Assert.DoesNotContain("AUTH", relay.Commands);
        Assert.Empty(relay.Mails);
    }

    /// <summary>
    /// Security (ADR-0053 decision 7): a certificate that is not the pinned one is refused as a failure of TLS, and no
    /// password is sent.
    /// </summary>
    [Fact]
    public async Task SendAsync_CertificateThatIsNotPinned_IsAFailureOfTls()
    {
        // Arrange
        await using FakeSmtpRelay relay = new();
        SmtpMailSender sender = new(Options(relay, thumbprint: new string('0', 64)));

        // Act
        MailSendResult result = await sender.SendAsync(Mail, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(MailSendResult.Failed(MailFailureReasons.Tls), result);
        Assert.Empty(relay.Commands);
    }

    /// <summary>
    /// Security (ADR-0053 decision 3): without a pin the certificate is checked against the store of the system and the
    /// host name, so the self-made certificate of the relay of the test is refused.
    /// </summary>
    [Fact]
    public async Task SendAsync_UntrustedCertificateWithoutAPin_IsAFailureOfTls()
    {
        // Arrange
        await using FakeSmtpRelay relay = new();
        SmtpMailSender sender = new(Options(relay, pinned: false));

        // Act
        MailSendResult result = await sender.SendAsync(Mail, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(MailSendResult.Failed(MailFailureReasons.Tls), result);
        Assert.Empty(relay.Commands);
    }

    /// <summary>Negative (ADR-0053 decision 5): a relay that refuses the account is a failure of the sign-in, and no mail goes.</summary>
    [Fact]
    public async Task SendAsync_RelayRefusesTheAccount_IsAFailureOfTheSignIn()
    {
        // Arrange
        await using FakeSmtpRelay relay = new() { RefusesAuthentication = true };
        SmtpMailSender sender = new(Options(relay));

        // Act
        MailSendResult result = await sender.SendAsync(Mail, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(MailSendResult.Failed(MailFailureReasons.Authentication), result);
        Assert.Empty(relay.Mails);
    }

    /// <summary>Negative (ADR-0053 decision 5): a relay that refuses the address is a refusal, and no mail goes.</summary>
    [Fact]
    public async Task SendAsync_RelayRefusesTheAddress_IsARefusal()
    {
        // Arrange
        await using FakeSmtpRelay relay = new() { RefusesRecipients = true };
        SmtpMailSender sender = new(Options(relay));

        // Act
        MailSendResult result = await sender.SendAsync(Mail, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(MailSendResult.Failed(MailFailureReasons.Refused), result);
        Assert.Empty(relay.Mails);
    }

    /// <summary>Negative (criterion 6, ADR-0053 decision 5): a relay that nothing answers for is a failure to connect.</summary>
    [Fact]
    public async Task SendAsync_NothingListens_IsAFailureToConnect()
    {
        // Arrange
        using TcpListener closed = new(IPAddress.Loopback, 0);
        closed.Start();
        int port = ((IPEndPoint)closed.LocalEndpoint).Port;
        closed.Stop();
        SmtpMailSender sender = new(new SmtpOptions
        {
            Host = IPAddress.Loopback.ToString(),
            Port = port,
            UserName = UserName,
            Password = Password,
            FromAddress = FromAddress,
            CertificateThumbprint = new string('0', 64),
        });

        // Act
        MailSendResult result = await sender.SendAsync(Mail, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(MailSendResult.Failed(MailFailureReasons.Connect), result);
    }

    /// <summary>
    /// Security (criterion 6, ASVS 1.3.11): a line break in the address or the subject is refused before any connection,
    /// so it can add no header.
    /// </summary>
    /// <param name="to">The address of the mail.</param>
    /// <param name="subject">The subject of the mail.</param>
    [Theory]
    [InlineData("anna@example.com\r\nBcc: eve@example.com", "Verify your booking")]
    [InlineData("anna@example.com", "Verify your booking\nBcc: eve@example.com")]
    public async Task SendAsync_LineBreakInTheAddressOrTheSubject_IsRefusedBeforeAnyConnection(string to, string subject)
    {
        // Arrange
        await using FakeSmtpRelay relay = new();
        SmtpMailSender sender = new(Options(relay));

        // Act and assert
        await Assert.ThrowsAsync<ArgumentException>(() => sender.SendAsync(new OutgoingMail(to, subject, "Body"), TestContext.Current.CancellationToken));
        Assert.Equal(0, relay.Connections);
    }

    /// <summary>
    /// Negative (ADR-0053 decision 1): a send that the caller cancels is cancelled for the caller, and is no failure of
    /// the relay, in the TLS handshake as well as while the greeting of the relay is awaited.
    /// </summary>
    /// <param name="implicitTls">TLS from the first byte, so the cancellation comes in the TLS handshake.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendAsync_CallerCancels_IsCancelled(bool implicitTls)
    {
        // Arrange
        await using FakeSmtpRelay relay = new(implicitTls) { Silent = true };
        SmtpMailSender sender = new(Options(relay, implicitTls ? SmtpSecurity.SslOnConnect : SmtpSecurity.StartTls));
        using CancellationTokenSource caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        caller.CancelAfter(TimeSpan.FromMilliseconds(300));

        // Act and assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sender.SendAsync(Mail, caller.Token));
    }

    /// <summary>
    /// Negative (ADR-0053 decisions 1 and 5): a relay that takes the connection and never answers is a timeout once the
    /// bound of the send runs out, in the TLS handshake as well as while the greeting of the relay is awaited.
    /// </summary>
    /// <param name="implicitTls">TLS from the first byte, so the bound runs out in the TLS handshake.</param>
    [Theory(Timeout = 20_000)]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendAsync_RelayNeverAnswers_IsATimeout(bool implicitTls)
    {
        // Arrange
        await using FakeSmtpRelay relay = new(implicitTls) { Silent = true };
        SmtpMailSender sender = new(Options(relay, implicitTls ? SmtpSecurity.SslOnConnect : SmtpSecurity.StartTls), ShortBound);

        // Act
        MailSendResult result = await sender.SendAsync(Mail, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(MailSendResult.Failed(MailFailureReasons.Timeout), result);
    }

    /// <summary>Negative (ADR-0053 decision 1): a bound of a send that is not positive is refused.</summary>
    [Fact]
    public void Constructor_BoundNotPositive_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new SmtpMailSender(new SmtpOptions(), TimeSpan.Zero));

    #endregion

    #region Helpers

    // The settings of the relay of the test, with its certificate pinned unless a test pins another one or none.
    private static SmtpOptions Options(
        FakeSmtpRelay relay,
        SmtpSecurity security = SmtpSecurity.SslOnConnect,
        string? host = null,
        int? port = null,
        string? forwarderHost = null,
        int? forwarderPort = null,
        string? thumbprint = null,
        bool pinned = true) => new()
        {
            Host = host ?? IPAddress.Loopback.ToString(),
            Port = port ?? relay.Port,
            Security = security,
            UserName = UserName,
            Password = Password,
            FromAddress = FromAddress,
            ForwarderHost = forwarderHost,
            ForwarderPort = forwarderPort,
            CertificateThumbprint = thumbprint ?? (pinned ? relay.Thumbprint : null),
        };

    #endregion
}
