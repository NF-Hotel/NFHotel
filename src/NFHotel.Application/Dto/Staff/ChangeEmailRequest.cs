namespace NFHotel.Application.Dto.Staff;

/// <summary>The e-mail address that a Manager sets for an account (MIL-059).</summary>
/// <param name="Email">The address; empty or <c>null</c> clears it.</param>
public sealed record ChangeEmailRequest(string? Email);
