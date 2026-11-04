namespace NFHotel.Application.Dto.Staff;

/// <summary>The name of an account, set by a Manager or by the member at the first sign-in (MIL-059).</summary>
/// <param name="FirstName">The first name.</param>
/// <param name="LastName">The last name.</param>
public sealed record RenameStaffRequest(string FirstName, string LastName);
