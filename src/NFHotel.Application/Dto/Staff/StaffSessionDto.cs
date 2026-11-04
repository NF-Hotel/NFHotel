using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Staff;

/// <summary>What a successful sign-in gives the web application.</summary>
/// <param name="Token">The signed token that the API wants in the staff header.</param>
/// <param name="ExpiresAt">When the token stops being valid; the cookie ends then too.</param>
/// <param name="UserName">The user name as stored.</param>
/// <param name="Role">The role.</param>
/// <param name="MustChangePassword">True when the member may do nothing but change the password (ADR-0017).</param>
/// <param name="FirstName">The first name, or <c>null</c> while the account has no name; the mobile app asks for it then (MIL-059).</param>
/// <param name="LastName">The last name, or <c>null</c> while the account has no name (MIL-059).</param>
public sealed record StaffSessionDto(
    string Token,
    DateTimeOffset ExpiresAt,
    string UserName,
    StaffRole Role,
    bool MustChangePassword = false,
    string? FirstName = null,
    string? LastName = null);
