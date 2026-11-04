using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Shifts;

/// <summary>A shift that a Manager plans or changes (MIL-060).</summary>
/// <param name="StartsAt">When it starts.</param>
/// <param name="EndsAt">When it ends.</param>
/// <param name="Role">The role it needs.</param>
/// <param name="AssigneeId">Who works it, or <c>null</c>.</param>
public sealed record SaveShiftRequest(DateTimeOffset StartsAt, DateTimeOffset EndsAt, StaffRole Role, int? AssigneeId);
