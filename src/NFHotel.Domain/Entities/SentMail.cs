using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Entities;

/// <summary>
/// The record of a mail (MIL-088, ADR-0052 decision 6): the key of the mailbox it goes to, its kind and its time, never
/// the email. It is written before the mail is sent and removed when the relay does not take the mail, so the mails per
/// mailbox and the fuse of ADR-0053 are counted from these records; the clean-up deletes a record after a day.
/// </summary>
public sealed class SentMail : Entity<int>
{
    /// <summary>For EF Core. Use <see cref="Record"/> in code.</summary>
    private SentMail()
    {
    }

    /// <summary>The key of the mailbox: the SHA-256 of its mailbox form, as 64 lower-case hex digits.</summary>
    public string EmailKey { get; private set; } = string.Empty;

    /// <summary>The kind of the mail.</summary>
    public MailKind Kind { get; private set; }

    /// <summary>When the mail was sent.</summary>
    public DateTimeOffset SentAt { get; private set; }

    /// <summary>Records a mail that is about to be sent.</summary>
    /// <param name="emailKey">The key of the mailbox, as <see cref="BookingRequest.EmailKey"/> has it.</param>
    /// <param name="kind">The kind of the mail.</param>
    /// <param name="sentAt">When it is sent.</param>
    /// <returns>The record, not yet stored.</returns>
    /// <exception cref="ArgumentException">The key is not 64 lower-case hex digits.</exception>
    public static SentMail Record(string emailKey, MailKind kind, DateTimeOffset sentAt)
    {
        BookingRequest.GuardHash(emailKey, nameof(emailKey));

        return new SentMail
        {
            EmailKey = emailKey,
            Kind = kind,
            SentAt = sentAt,
        };
    }
}
