namespace NFHotel.Application.Dto.Staff;

/// <summary>The temporary password that a Manager sets for another account (ADR-0017).</summary>
/// <param name="NewPassword">The temporary password; the person must change it at the next sign-in.</param>
public sealed record ResetPasswordRequest(string NewPassword);
