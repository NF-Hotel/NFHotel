namespace NFHotel.Application.Dto.Staff;

/// <summary>One page of the audit trail, newest first (ADR-0015).</summary>
/// <param name="Items">The lines of this page.</param>
/// <param name="Page">The page number, from 1.</param>
/// <param name="PageSize">The number of lines on a full page.</param>
/// <param name="Total">The number of lines in the whole trail.</param>
public sealed record StaffAuditPageDto(IReadOnlyList<StaffAuditEventDto> Items, int Page, int PageSize, int Total);
