using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NFHotel.Domain.Entities;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core mapping for <see cref="SentMail"/> (MIL-088, ADR-0052 decision 6 and ADR-0053 decision 6): the record of a mail
/// that the limits per mailbox and the fuse count, with the SHA-256 of the mailbox and never the email.
/// </summary>
public sealed class SentMailConfiguration : EntityConfiguration<SentMail, int>
{
    #region Properties

    /// <inheritdoc />
    protected override string TableName => "sent_mail";

    #endregion Properties

    #region Protected methods

    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<SentMail> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Property(mail => mail.EmailKey)
            .HasMaxLength(BookingRequest.HashLength)
            .IsRequired();

        builder.Property(mail => mail.Kind)
            .HasConversion<int>()
            .IsRequired();

        // The limits per mailbox count the mails of one key and kind in a window; the fuse and the clean-up read the time.
        builder.HasIndex(mail => new { mail.EmailKey, mail.Kind, mail.SentAt })
            .HasDatabaseName("ix_sent_mail_email_key_kind_sent_at");

        builder.HasIndex(mail => mail.SentAt)
            .HasDatabaseName("ix_sent_mail_sent_at");
    }

    #endregion Protected methods
}
