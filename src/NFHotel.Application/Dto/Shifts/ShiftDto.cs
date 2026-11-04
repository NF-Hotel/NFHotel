using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Shifts;

/// <summary>A shift (MIL-060).</summary>
/// <param name="Id">The shift.</param>
/// <param name="StartsAt">When it starts.</param>
/// <param name="EndsAt">When it ends.</param>
/// <param name="Role">The role it needs.</param>
/// <param name="AssigneeId">Who works it, or <c>null</c>.</param>
/// <param name="AssigneeName">The name of that member of staff, or <c>null</c>.</param>
public sealed record ShiftDto(int Id, DateTimeOffset StartsAt, DateTimeOffset EndsAt, StaffRole Role, int? AssigneeId, string? AssigneeName);
