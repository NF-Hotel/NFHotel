using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Staff;

/// <summary>The role that a Manager gives an account (MIL-059).</summary>
/// <param name="Role">The new role.</param>
public sealed record ChangeRoleRequest(StaffRole Role);
