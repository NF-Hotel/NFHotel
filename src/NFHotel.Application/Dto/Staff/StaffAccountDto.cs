using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Staff;

/// <summary>An account as the staff page lists it; it never holds the hash (ADR-0015).</summary>
/// <param name="Id">The key.</param>
/// <param name="UserName">The user name.</param>
/// <param name="Role">The role.</param>
/// <param name="IsActive">False when the account is disabled.</param>
/// <param name="MustChangePassword">True when the person has to choose a new password.</param>
public sealed record StaffAccountDto(int Id, string UserName, StaffRole Role, bool IsActive, bool MustChangePassword);
