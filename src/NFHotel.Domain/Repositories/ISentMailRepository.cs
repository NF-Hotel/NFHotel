using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Repositories;

/// <summary>
/// Storage of the records of the mails (MIL-088, ADR-0052 decision 6 and ADR-0053 decision 6). The limits of the mails per
/// mailbox and the fuse are counted from it. <see cref="Add"/> and <see cref="Remove"/> mark and do not persist;
/// <see cref="DeleteOlderThanAsync"/>, the clean-up, deletes in the database at once.
/// </summary>
public interface ISentMailRepository
{
    #region Public methods

    /// <summary>Marks a record for insertion. Does not persist.</summary>
    /// <param name="mail">The record.</param>
    void Add(SentMail mail);

    /// <summary>Marks a record for deletion: the relay did not take its mail. Does not persist.</summary>
    /// <param name="mail">The record, as it was added.</param>
    void Remove(SentMail mail);

    /// <summary>Counts the mails of one kind to one mailbox after a moment.</summary>
    /// <param name="emailKey">The key of the mailbox.</param>
    /// <param name="kind">The kind of the mails.</param>
    /// <param name="since">The moment; a mail sent at it or before is not counted.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The number of mails.</returns>
    Task<int> CountForEmailSinceAsync(string emailKey, MailKind kind, DateTimeOffset since, CancellationToken cancellationToken = default);

    /// <summary>Counts every mail after a moment, of every kind and to every mailbox: the count of the fuse.</summary>
    /// <param name="since">The moment; a mail sent at it or before is not counted.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The number of mails.</returns>
    Task<int> CountSinceAsync(DateTimeOffset since, CancellationToken cancellationToken = default);

    /// <summary>Deletes, in the database at once, every record of a mail sent at or before a moment.</summary>
    /// <param name="cutoff">The moment.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The number of records deleted.</returns>
    Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default);

    #endregion Public methods
}
