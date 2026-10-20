using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Staff;

/// <summary>One line of the audit trail as the audit page shows it (ADR-0015).</summary>
/// <param name="OccurredAt">When it happened (UTC).</param>
/// <param name="ActorName">Who did it, or <c>null</c>.</param>
/// <param name="Action">What happened.</param>
/// <param name="TargetName">To whom, or <c>null</c>.</param>
/// <param name="Outcome"><c>Success</c> or the error code.</param>
/// <param name="RemoteAddress">The address of the request, or <c>null</c>.</param>
public sealed record StaffAuditEventDto(
    DateTimeOffset OccurredAt,
    string? ActorName,
    StaffAuditAction Action,
    string? TargetName,
    string Outcome,
    string? RemoteAddress);
