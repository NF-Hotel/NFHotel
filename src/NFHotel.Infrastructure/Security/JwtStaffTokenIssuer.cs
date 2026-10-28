using System.Text;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Infrastructure.Config;

namespace NFHotel.Infrastructure.Security;

/// <summary>Issues the HS256 token of a signed-in member of staff, with the id, the name and the role as claims (ADR-0005).</summary>
public sealed class JwtStaffTokenIssuer : IStaffTokenIssuer
{
    /// <summary>The claim with the role.</summary>
    public const string RoleClaim = "role";

    /// <summary>The claim with the user name.</summary>
    public const string NameClaim = "name";

    /// <summary>The claim with the security stamp of the account at the sign-in (ADR-0017).</summary>
    public const string StampClaim = "stamp";

    /// <summary>The claim that is <c>true</c> when the member may do nothing but change the password (ADR-0017).</summary>
    public const string MustChangeClaim = "must_change";

    private readonly StaffSecurityOptions _options;
    private readonly IClock _clock;
    private readonly SigningCredentials _credentials;

    /// <summary>Creates the issuer.</summary>
    /// <param name="options">The signing key and the lifetime.</param>
    /// <param name="clock">The source of the time, so tests can fix it.</param>
    public JwtStaffTokenIssuer(StaffSecurityOptions options, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clock);

        _options = options;
        _clock = clock;
        _credentials = new SigningCredentials(SigningKeyFor(options), SecurityAlgorithms.HmacSha256);
    }

    /// <summary>Makes the symmetric key that signs and validates the tokens.</summary>
    /// <param name="options">The options with the signing key.</param>
    /// <returns>The key.</returns>
    public static SymmetricSecurityKey SigningKeyFor(StaffSecurityOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
    }

    /// <inheritdoc />
    public IssuedStaffToken Issue(StaffMember member)
    {
        ArgumentNullException.ThrowIfNull(member);

        var now = _clock.UtcNow;
        var expires = now.AddMinutes(_options.TokenLifetimeMinutes);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = StaffSecurityOptions.Issuer,
            Audience = StaffSecurityOptions.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = _credentials,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = member.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [NameClaim] = member.UserName,
                [RoleClaim] = member.Role.ToString(),
                [StampClaim] = member.SecurityStamp,
                [MustChangeClaim] = member.MustChangePassword,
            },
        };

        return new IssuedStaffToken(new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }
}
