using NFHotel.Domain.Entities;

namespace NFHotel.Application.Ports;

/// <summary>Makes the signed token that proves a sign-in to the API (ADR-0005). Implemented in Infrastructure.</summary>
public interface IStaffTokenIssuer
{
    /// <summary>Issues a token for a member of staff.</summary>
    /// <param name="member">The member who has proved the password.</param>
    /// <returns>The token and the instant it stops being valid.</returns>
    IssuedStaffToken Issue(StaffMember member);
}

/// <summary>A signed token and its expiry.</summary>
/// <param name="Token">The token.</param>
/// <param name="ExpiresAt">When it stops being valid.</param>
public sealed record IssuedStaffToken(string Token, DateTimeOffset ExpiresAt);