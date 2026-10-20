namespace NFHotel.Application.Dto.Staff;

/// <summary>The fields of a change of the own password (ADR-0015).</summary>
/// <param name="CurrentPassword">The password that is replaced; it proves that the caller knows it.</param>
/// <param name="NewPassword">The new password.</param>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
