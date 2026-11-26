using System.Text.RegularExpressions;

using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.TestSupport;

/// <summary>
/// The booking requests in memory (MIL-088): a claim removes the open request of the hash at once, as the one statement of
/// the database does, and the clean-up removes the lapsed ones.
/// </summary>
public sealed class InMemoryBookingRequestRepository : InMemoryRepository<BookingRequest>, IBookingRequestRepository
{
    #region Properties

    /// <summary>The requests that a verification claimed, in the order claimed.</summary>
    public List<BookingRequest> Claimed { get; } = [];

    #endregion Properties

    #region Public methods

    /// <inheritdoc />
    public Task<BookingRequest?> FindByEmailKeyAsync(string emailKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(request => request.EmailKey == emailKey));

    /// <inheritdoc />
    public Task<BookingRequest?> ClaimOpenAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        BookingRequest? request = Items.SingleOrDefault(item => item.TokenHash == tokenHash && item.ExpiresAt > now);
        if (request is not null)
        {
            Items.Remove(request);
            Claimed.Add(request);
        }

        return Task.FromResult(request);
    }

    /// <inheritdoc />
    public Task<int> DeleteLapsedAsync(DateTimeOffset now, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.RemoveAll(request => request.ExpiresAt <= now));

    #endregion Public methods
}

/// <summary>The records of the mails in memory (MIL-088): counted per mailbox and kind, and in all for the fuse.</summary>
public sealed class InMemorySentMailRepository : InMemoryRepository<SentMail>, ISentMailRepository
{
    #region Public methods

    /// <inheritdoc />
    public Task<int> CountForEmailSinceAsync(string emailKey, MailKind kind, DateTimeOffset since, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Count(mail => mail.EmailKey == emailKey && mail.Kind == kind && mail.SentAt > since));

    /// <inheritdoc />
    public Task<int> CountSinceAsync(DateTimeOffset since, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Count(mail => mail.SentAt > since));

    /// <inheritdoc />
    public Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.RemoveAll(mail => mail.SentAt <= cutoff));

    /// <summary>Stores a record of a mail sent at a moment, as an earlier request would have left it.</summary>
    /// <param name="email">The email the mail went to.</param>
    /// <param name="kind">The kind of the mail.</param>
    /// <param name="sentAt">When it was sent.</param>
    public void AddSent(string email, MailKind kind, DateTimeOffset sentAt) =>
        Add(SentMail.Record(BookingRequestTestData.KeyOf(email), kind, sentAt));

    #endregion Public methods
}

/// <summary>
/// A relay that keeps the mails it takes, or refuses them with a reason, so a test reads the link of a mail and lets the
/// relay fail (MIL-088, ADR-0053). It takes mails from parallel requests; a test reads the lists once they are done.
/// </summary>
public sealed class FakeMailSender : IMailSender
{
    #region Fields

    private readonly Lock _gate = new();

    #endregion Fields

    #region Properties

    /// <summary>The mails the relay took, in the order sent.</summary>
    public List<OutgoingMail> Sent { get; } = [];

    /// <summary>Every mail the relay was given, taken or not.</summary>
    public List<OutgoingMail> Attempts { get; } = [];

    /// <summary>Decides, for each mail, the reason the relay refuses it; <see langword="null"/> takes it.</summary>
    public Func<OutgoingMail, string?> Refuse { get; set; } = _ => null;

    /// <summary>
    /// Decides, for each mail, an error that the send ends with instead of a result, as a failure that is no answer of the
    /// relay would (RC-436 F1); <see langword="null"/> lets <see cref="Refuse"/> decide.
    /// </summary>
    public Func<OutgoingMail, Exception?> Throw { get; set; } = _ => null;

    #endregion Properties

    #region Public methods

    /// <summary>Makes the relay refuse every mail with a reason.</summary>
    /// <param name="reason">The reason, from <see cref="MailFailureReasons"/>.</param>
    public void RefuseAll(string reason) => Refuse = _ => reason;

    /// <inheritdoc />
    public Task<MailSendResult> SendAsync(OutgoingMail mail, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mail);

        lock (_gate)
        {
            Attempts.Add(mail);
            if (Throw(mail) is { } error)
            {
                return Task.FromException<MailSendResult>(error);
            }

            if (Refuse(mail) is { } reason)
            {
                return Task.FromResult(MailSendResult.Failed(reason));
            }

            Sent.Add(mail);
            return Task.FromResult(MailSendResult.Success);
        }
    }

    /// <summary>The token of the link in the last mail that the relay took.</summary>
    /// <returns>The token after the <c>#</c> of the link.</returns>
    public string LastToken() => BookingRequestTestData.TokenIn(Sent[^1]);

    #endregion Public methods
}

/// <summary>The setting of the verification by email, set by the test (MIL-088, ADR-0052 decision 8).</summary>
public sealed class FakeBookingVerificationSettings : IBookingVerificationSettings
{
    #region Properties

    /// <inheritdoc />
    public bool Enabled { get; set; }

    /// <inheritdoc />
    public Uri? SiteAddress { get; set; } = new("https://hotel.example.com/");

    /// <inheritdoc />
    public int MaxMailsPerDay { get; set; } = 400;

    #endregion Properties
}

/// <summary>
/// Keeps the security events of the booking request as short lines, so a test reads what was written. It takes events
/// from parallel requests; a test reads the lines once they are done.
/// </summary>
public sealed class RecordingBookingRequestEvents : IBookingRequestEvents
{
    #region Fields

    private readonly Lock _gate = new();

    #endregion Fields

    #region Properties

    /// <summary>The events, in the order written.</summary>
    public List<string> Lines { get; } = [];

    #endregion Properties

    #region Public methods

    /// <inheritdoc />
    public void RequestMade() => Write("request_made");

    /// <inheritdoc />
    public void VerificationFailed() => Write("verification_failed");

    /// <inheritdoc />
    public void PublicBookingMade() => Write("public_booking_made");

    /// <inheritdoc />
    public void LimitRefused(string limit) => Write($"limit_refused {limit}");

    /// <inheritdoc />
    public void MailSent(MailKind kind) => Write($"mail_sent {kind}");

    /// <inheritdoc />
    public void MailNotSent(MailKind kind, string reason) => Write($"mail_not_sent {kind} {reason}");

    /// <inheritdoc />
    public void MailFuseBlown() => Write("mail_fuse_blown");

    #endregion Public methods

    #region Private methods

    private void Write(string line)
    {
        lock (_gate)
        {
            Lines.Add(line);
        }
    }

    #endregion Private methods
}

/// <summary>The values that the tests of the booking request share (MIL-088).</summary>
public static partial class BookingRequestTestData
{
    #region Public methods

    /// <summary>The key of the mailbox of an email, as the use case computes it: the SHA-256 of its mailbox form.</summary>
    /// <param name="email">The email.</param>
    /// <returns>64 lower-case hex digits.</returns>
    public static string KeyOf(string email) => HashOf(Mailbox.NormalFormOf(email));

    /// <summary>The SHA-256 of a text in UTF-8, as 64 lower-case hex digits: the hash of a token or of a mailbox form.</summary>
    /// <param name="text">The text.</param>
    /// <returns>64 lower-case hex digits.</returns>
    public static string HashOf(string text) =>
        Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)));

    /// <summary>The token of the link in a mail.</summary>
    /// <param name="mail">The mail.</param>
    /// <returns>The 43 characters after <c>booking/verify#</c>.</returns>
    /// <exception cref="InvalidOperationException">The mail holds no link.</exception>
    public static string TokenIn(OutgoingMail mail)
    {
        ArgumentNullException.ThrowIfNull(mail);

        Match match = LinkPattern().Match(mail.Body);
        return match.Success
            ? match.Groups["token"].Value
            : throw new InvalidOperationException("The mail holds no link to verify a booking.");
    }

    #endregion Public methods

    #region Private methods

    [GeneratedRegex(@"/booking/verify#(?<token>[A-Za-z0-9_-]{43})(?![A-Za-z0-9_-])")]
    private static partial Regex LinkPattern();

    #endregion Private methods
}
