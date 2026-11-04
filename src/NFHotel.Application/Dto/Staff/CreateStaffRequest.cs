using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Staff;

/// <summary>The fields of a new staff account.</summary>
/// <param name="UserName">The user name.</param>
/// <param name="Password">The password in clear text; hashed before it is stored.</param>
/// <param name="Role">The role.</param>
/// <param name="MustChangePassword">True when the person must choose a new password at the first sign-in (ADR-0017).</param>
/// <param name="FirstName">The first name, or <c>null</c>; given together with <paramref name="LastName"/> (MIL-059).</param>
/// <param name="LastName">The last name, or <c>null</c>; given together with <paramref name="FirstName"/> (MIL-059).</param>
/// <param name="Email">The e-mail address, or <c>null</c> (MIL-059).</param>
public sealed record CreateStaffRequest(
    string UserName,
    string Password,
    StaffRole Role,
    bool MustChangePassword = false,
    string? FirstName = null,
    string? LastName = null,
    string? Email = null);
