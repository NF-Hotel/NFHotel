using System.Globalization;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Infrastructure.Config;

namespace NFHotel.Infrastructure.Security;

/// <summary>
/// Issues the HS256 token of a signed-in guest (MIL-018, ADR-0047): the role Guest and the key of one booking as claims,
/// valid for two hours, signed with the key of the staff tokens (ADR-0005). Its audience is not the audience of a staff
/// token, so neither kind of token passes for the other.
/// </summary>
public sealed class JwtGuestTokenIssuer : IGuestTokenIssuer
{
    #region Fields

    /// <summary>The audience of a guest token.</summary>
    public const string Audience = "nfhotel-guest";

    /// <summary>The claim with the key of the booking that the token opens.</summary>
    public const string BookingClaim = "booking";

    /// <summary>How long a guest token is valid (ADR-0047 decision 2).</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(2);

    private readonly IClock _clock;
    private readonly SigningCredentials _credentials;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the issuer.</summary>
    /// <param name="options">The signing key, the same key as the staff tokens.</param>
    /// <param name="clock">The source of the time, so tests can fix it.</param>
    public JwtGuestTokenIssuer(StaffSecurityOptions options, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clock);

        _clock = clock;
        _credentials = new SigningCredentials(JwtStaffTokenIssuer.SigningKeyFor(options), SecurityAlgorithms.HmacSha256);
    }

    #endregion Constructors

    #region Public methods

    /// <inheritdoc />
    /// <remarks>
    /// The token has no subject (<c>sub</c>) claim on purpose: the API reads the subject of a staff token as the key of a
    /// staff account, and a guest is none.
    /// </remarks>
    public GuestSessionDto Issue(int bookingId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bookingId);

        DateTimeOffset now = _clock.UtcNow;
        DateTimeOffset expires = now.Add(Lifetime);
        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = StaffSecurityOptions.Issuer,
            Audience = Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = _credentials,
            Claims = new Dictionary<string, object>
            {
                [JwtStaffTokenIssuer.RoleClaim] = Roles.Guest,
                [BookingClaim] = bookingId.ToString(CultureInfo.InvariantCulture),
            },
        };

        return new GuestSessionDto(new JsonWebTokenHandler().CreateToken(descriptor), expires, bookingId);
    }

    #endregion Public methods
}
