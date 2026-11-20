using Microsoft.EntityFrameworkCore;

using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Infrastructure.Repositories;

/// <summary>
/// Stores the records of the mails that the limits per mailbox and the fuse count (MIL-088, ADR-0052 decision 6 and
/// ADR-0053 decision 6). A window is rolling: a mail counts while its time is after the start of the window.
/// </summary>
public sealed class SentMailRepository : Repository<SentMail, int>, ISentMailRepository
{
    #region Constructors

    /// <summary>Creates the repository.</summary>
    /// <param name="context">The database context.</param>
    public SentMailRepository(HotelDbContext context)
        : base(context)
    {
    }

    #endregion Constructors

    #region Public methods

    /// <inheritdoc />
    public void Remove(SentMail mail) => MarkForRemoval(mail);

    /// <inheritdoc />
    public async Task<int> CountForEmailSinceAsync(string emailKey, MailKind kind, DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(emailKey);

        return await Context.SentMails
            .CountAsync(mail => mail.EmailKey == emailKey && mail.Kind == kind && mail.SentAt > since, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> CountSinceAsync(DateTimeOffset since, CancellationToken cancellationToken = default) =>
        await Context.SentMails
            .CountAsync(mail => mail.SentAt > since, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default) =>
        await Context.SentMails
            .Where(mail => mail.SentAt <= cutoff)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

    #endregion Public methods
}
