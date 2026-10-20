using NFHotel.Domain.Entities;

namespace NFHotel.Domain.Repositories;

/// <summary>
/// Storage of the audit trail of the staff accounts (ADR-0017). It can add and read; there is no update and no delete.
/// </summary>
public interface IStaffAuditRepository
{
    /// <summary>Marks a record for insertion. It is saved with the change it describes by the unit of work.</summary>
    /// <param name="auditEvent">The record.</param>
    void Add(StaffAuditEvent auditEvent);

    /// <summary>Reads one page of the trail, newest first.</summary>
    /// <param name="skip">How many records to skip.</param>
    /// <param name="take">How many records to return.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The records.</returns>
    Task<IReadOnlyList<StaffAuditEvent>> ListAsync(int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>Counts all records.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The number of records.</returns>
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
