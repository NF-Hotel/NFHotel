using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace NFHotel.Infrastructure.Tests.Mail;

/// <summary>
/// A relay of the tests on the loopback address (MIL-088, ADR-0053 decision 7): it speaks enough SMTP for one mail, with
/// TLS from the first byte or after STARTTLS, a certificate of its own for the host name <see cref="HostName"/>, and the
/// refusals that a test asks for. It keeps the commands and the mails it took, so a test reads what the adapter sent.
/// </summary>
public sealed class FakeSmtpRelay : IAsyncDisposable
{
    #region Fields

    /// <summary>The host name in the certificate of the relay.</summary>
    public const string HostName = "relay.test";

    private static readonly DateTimeOffset ValidFrom = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ValidTo = new(2099, 12, 31, 0, 0, 0, TimeSpan.Zero);

    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly X509Certificate2 _certificate;
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _accepting;
    private int _connections;

    #endregion Fields

    #region Constructors

    /// <summary>Starts the relay.</summary>
    /// <param name="implicitTls">TLS from the first byte; otherwise plain text until STARTTLS.</param>
    public FakeSmtpRelay(bool implicitTls = true)
    {
        ImplicitTls = implicitTls;
        _certificate = NewCertificate();
        Thumbprint = Convert.ToHexStringLower(SHA256.HashData(_certificate.RawData));
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _accepting = AcceptAsync(_stop.Token);
    }

    #endregion Constructors

    #region Properties

    /// <summary>The port of the relay on the loopback address.</summary>
    public int Port { get; }

    /// <summary>The SHA-256 of the DER form of the certificate, as <c>Smtp:CertificateThumbprint</c> pins it.</summary>
    public string Thumbprint { get; }

    /// <summary>Whether TLS runs from the first byte.</summary>
    public bool ImplicitTls { get; }

    /// <summary>Whether the relay offers STARTTLS on a plain connection.</summary>
    public bool OffersStartTls { get; init; } = true;

    /// <summary>Whether the relay refuses every user name and password.</summary>
    public bool RefusesAuthentication { get; init; }

    /// <summary>Whether the relay refuses every recipient.</summary>
    public bool RefusesRecipients { get; init; }

    /// <summary>Whether the relay takes the connection and never answers.</summary>
    public bool Silent { get; init; }

    /// <summary>Whether the relay takes the mail and then never answers the quit (RC-436 F3).</summary>
    public bool SilentAtTheQuit { get; init; }

    /// <summary>Every command the relay was sent, in order, with the data of AUTH left out.</summary>
    public ConcurrentQueue<string> Commands { get; } = new();

    /// <summary>The user names that signed in.</summary>
    public ConcurrentQueue<string> UserNames { get; } = new();

    /// <summary>The mails the relay took, each as the text after DATA.</summary>
    public ConcurrentQueue<string> Mails { get; } = new();

    /// <summary>The host names that the clients asked for in TLS.</summary>
    public ConcurrentQueue<string> TlsHostNames { get; } = new();

    /// <summary>The number of connections the relay took.</summary>
    public int Connections => _connections;

    #endregion Properties

    #region Public methods

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        _listener.Stop();
        try
        {
            await _accepting;
        }
        catch (Exception exception) when (exception is OperationCanceledException or SocketException or ObjectDisposedException)
        {
            // The relay is stopping.
        }

        _stop.Dispose();
        _certificate.Dispose();
    }

    #endregion Public methods

    #region Private methods

    // A certificate for HostName, made for the test and loaded from its PKCS #12 form, which TLS on Windows needs. Its
    // dates are fixed and wide, so the test reads no clock.
    private static X509Certificate2 NewCertificate()
    {
        using RSA key = RSA.Create(2048);
        CertificateRequest request = new($"CN={HostName}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        SubjectAlternativeNameBuilder names = new();
        names.AddDnsName(HostName);
        request.CertificateExtensions.Add(names.Build());
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.1")], false));
        using X509Certificate2 made = request.CreateSelfSigned(ValidFrom, ValidTo);
        return X509CertificateLoader.LoadPkcs12(made.Export(X509ContentType.Pfx), null);
    }

    private async Task AcceptAsync(CancellationToken stop)
    {
        while (!stop.IsCancellationRequested)
        {
            TcpClient client = await _listener.AcceptTcpClientAsync(stop);
            Interlocked.Increment(ref _connections);
            _ = Task.Run(() => ServeAsync(client, stop), stop);
        }
    }

    private async Task ServeAsync(TcpClient client, CancellationToken stop)
    {
        using (client)
        {
            try
            {
                Stream stream = client.GetStream();
                if (Silent)
                {
                    // Holds the connection without a word until the relay stops.
                    await new TaskCompletionSource().Task.WaitAsync(stop);
                }

                if (ImplicitTls)
                {
                    stream = await SecureAsync(stream, stop);
                }

                await ConverseAsync(stream, stop);
            }
            catch (Exception exception) when (exception is IOException or OperationCanceledException or ObjectDisposedException
                or System.Security.Authentication.AuthenticationException)
            {
                // The client went away, refused the certificate, or the relay is stopping.
            }
        }
    }

    private async Task<SslStream> SecureAsync(Stream stream, CancellationToken stop)
    {
        SslStream secure = new(stream, leaveInnerStreamOpen: false);
        await secure.AuthenticateAsServerAsync(
            new SslServerAuthenticationOptions { ServerCertificate = _certificate, ClientCertificateRequired = false },
            stop);
        TlsHostNames.Enqueue(secure.TargetHostName);
        return secure;
    }

    private async Task ConverseAsync(Stream stream, CancellationToken stop)
    {
        bool secure = ImplicitTls;
        Line line = new(stream);
        await line.WriteAsync($"220 {HostName} ESMTP", stop);

        while (await line.ReadAsync(stop) is { } command)
        {
            string verb = command.Split(' ', 2)[0].ToUpperInvariant();
            Commands.Enqueue(verb == "AUTH" ? "AUTH" : command);
            switch (verb)
            {
                case "EHLO":
                    await line.WriteAsync(Capabilities(secure), stop);
                    break;
                case "STARTTLS" when !secure && OffersStartTls:
                    await line.WriteAsync("220 2.0.0 Ready to start TLS", stop);
                    stream = await SecureAsync(stream, stop);
                    line = new Line(stream);
                    secure = true;
                    break;
                case "AUTH" when secure:
                    await AuthenticateAsync(line, command, stop);
                    break;
                case "MAIL":
                case "RSET":
                case "NOOP":
                    await line.WriteAsync("250 2.0.0 OK", stop);
                    break;
                case "RCPT":
                    await line.WriteAsync(RefusesRecipients ? "550 5.1.1 The mailbox does not exist" : "250 2.1.5 OK", stop);
                    break;
                case "DATA":
                    await line.WriteAsync("354 End data with <CR><LF>.<CR><LF>", stop);
                    Mails.Enqueue(await ReadDataAsync(line, stop));
                    await line.WriteAsync("250 2.0.0 OK queued", stop);
                    break;
                case "QUIT" when SilentAtTheQuit:
                    // Holds the connection without a word until the relay stops.
                    await new TaskCompletionSource().Task.WaitAsync(stop);
                    return;
                case "QUIT":
                    await line.WriteAsync("221 2.0.0 Bye", stop);
                    return;
                default:
                    await line.WriteAsync("502 5.5.1 Not implemented", stop);
                    break;
            }
        }
    }

    // Before TLS a relay offers STARTTLS and no sign-in, as a relay that takes a password does.
    private string Capabilities(bool secure) =>
        secure
            ? $"250-{HostName}\r\n250-AUTH PLAIN LOGIN\r\n250 8BITMIME"
            : OffersStartTls ? $"250-{HostName}\r\n250 STARTTLS" : $"250 {HostName}";

    private async Task AuthenticateAsync(Line line, string command, CancellationToken stop)
    {
        string[] parts = command.Split(' ');
        string mechanism = parts.Length > 1 ? parts[1].ToUpperInvariant() : string.Empty;
        string? userName = null;
        if (mechanism == "PLAIN")
        {
            string response = parts.Length > 2 ? parts[2] : await AskAsync(line, string.Empty, stop);
            userName = Encoding.UTF8.GetString(Convert.FromBase64String(response)).Split('\0')[1];
        }
        else if (mechanism == "LOGIN")
        {
            string first = parts.Length > 2 ? parts[2] : await AskAsync(line, "VXNlcm5hbWU6", stop);
            userName = Encoding.UTF8.GetString(Convert.FromBase64String(first));
            await AskAsync(line, "UGFzc3dvcmQ6", stop);
        }

        if (userName is null || RefusesAuthentication)
        {
            await line.WriteAsync("535 5.7.8 Authentication credentials invalid", stop);
            return;
        }

        UserNames.Enqueue(userName);
        await line.WriteAsync("235 2.7.0 Authentication successful", stop);
    }

    private static async Task<string> AskAsync(Line line, string challenge, CancellationToken stop)
    {
        await line.WriteAsync($"334 {challenge}", stop);
        return await line.ReadAsync(stop) ?? string.Empty;
    }

    private static async Task<string> ReadDataAsync(Line line, CancellationToken stop)
    {
        StringBuilder data = new();
        while (await line.ReadAsync(stop) is { } text && text != ".")
        {
            data.Append(text.StartsWith("..", StringComparison.Ordinal) ? text[1..] : text).Append('\n');
        }

        return data.ToString();
    }

    #endregion Private methods

    #region Nested types

    // Reads and writes the lines of SMTP, which end in CR LF, on one stream.
    private sealed class Line(Stream stream)
    {
        private readonly StreamReader _reader = new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);

        public async Task<string?> ReadAsync(CancellationToken stop) => await _reader.ReadLineAsync(stop);

        public async Task WriteAsync(string text, CancellationToken stop)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text + "\r\n");
            await stream.WriteAsync(bytes, stop);
            await stream.FlushAsync(stop);
        }
    }

    #endregion Nested types
}
