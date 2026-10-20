using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Staff;

/// <summary>
/// Adds the audit record of an act on the staff accounts (ADR-0015). The record is only marked for insertion; the
/// unit of work saves it in the same transaction as the change it describes.
/// </summary>
internal sealed class StaffAuditRecorder
{
    /// <summary>The outcome text of an act that succeeded.</summary>
    public const string Success = "Success";

    /// <summary>The actor name of a command run on the server.</summary>
    public const string SystemActor = "system";

    private readonly IStaffAuditRepository _audit;
    private readonly IStaffCallContext _context;
    private readonly IClock _clock;

    public StaffAuditRecorder(IStaffAuditRepository audit, IStaffCallContext context, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(clock);

        _audit = audit;
        _context = context;
        _clock = clock;
    }

    /// <summary>Records an act of the signed-in caller (or of the server when nobody is signed in) on a target.</summary>
    public void ByCaller(StaffAuditAction action, string outcome, StaffMember? target, bool withTargetId = true)
    {
        var actorName = _context.StaffName ?? (_context.StaffId is null ? SystemActor : null);
        _audit.Add(StaffAuditEvent.Create(
            _clock.UtcNow, action, outcome, _context.StaffId, actorName, withTargetId ? target?.Id : null, target?.UserName, _context.RemoteAddress));
    }

    /// <summary>Records an act of an account on itself, for example a sign-in, where nobody is signed in yet.</summary>
    public void BySelf(StaffAuditAction action, string outcome, StaffMember member) =>
        _audit.Add(StaffAuditEvent.Create(
            _clock.UtcNow, action, outcome, member.Id, member.UserName, member.Id, member.UserName, _context.RemoteAddress));
}
