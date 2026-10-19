using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Staff;

/// <summary>What a successful sign-in gives the web application.</summary>
/// <param name="Token">The signed token that the API wants in the staff header.</param>
/// <param name="ExpiresAt">When the token stops being valid; the cookie ends then too.</param>
/// <param name="UserName">The user name as stored.</param>
/// <param name="Role">The role.</param>
public sealed record StaffSessionDto(string Token, DateTimeOffset ExpiresAt, string UserName, StaffRole Role);
