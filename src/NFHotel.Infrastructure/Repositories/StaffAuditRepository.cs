using Microsoft.EntityFrameworkCore;

using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IStaffAuditRepository"/>. It adds and reads; it never updates or deletes.</summary>
public sealed class StaffAuditRepository : IStaffAuditRepository
{
    private readonly HotelDbContext _context;

    /// <summary>Creates the repository.</summary>
    /// <param name="context">The context.</param>
    public StaffAuditRepository(HotelDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public void Add(StaffAuditEvent auditEvent)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        _context.StaffAuditEvents.Add(auditEvent);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StaffAuditEvent>> ListAsync(int skip, int take, CancellationToken cancellationToken = default) =>
        await _context.StaffAuditEvents
            .AsNoTracking()
            .OrderByDescending(auditEvent => auditEvent.OccurredAt)
            .ThenByDescending(auditEvent => auditEvent.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        await _context.StaffAuditEvents.CountAsync(cancellationToken).ConfigureAwait(false);
}
