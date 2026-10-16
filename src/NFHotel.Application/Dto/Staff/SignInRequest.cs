namespace NFHotel.Application.Dto.Staff;

/// <summary>The user name and password that a member of staff types.</summary>
/// <param name="UserName">The user name; case does not matter.</param>
/// <param name="Password">The password.</param>
public sealed record SignInRequest(string UserName, string Password);