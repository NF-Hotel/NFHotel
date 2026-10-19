using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Staff;

/// <summary>The fields of a new staff account.</summary>
/// <param name="UserName">The user name.</param>
/// <param name="Password">The password in clear text; hashed before it is stored.</param>
/// <param name="Role">The role.</param>
public sealed record CreateStaffRequest(string UserName, string Password, StaffRole Role);
