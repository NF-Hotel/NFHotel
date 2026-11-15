using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Security;

namespace NFHotel.Api.Security;

/// <summary>
/// The rules that every token of the API is validated with (ADR-0005, and ADR-0047 decision 2: one token format and one
/// key): the issuer and the signing key of the staff tokens, HS256 only, a required expiry and 30 seconds of clock skew.
/// The staff token and the guest token differ only in their audience and in the header they arrive in, so a change of
/// the format is made here once for both.
/// </summary>
public static class ApiTokenValidation
{
    #region Fields

    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    #endregion Fields

    #region Public methods

    /// <summary>The validation parameters of a token for one audience.</summary>
    /// <param name="staff">The security options that hold the signing key.</param>
    /// <param name="audience">The audience of the token: the one of the staff token or the one of the guest token.</param>
    /// <returns>New parameters, to which a scheme may add the claim types it reads.</returns>
    public static TokenValidationParameters For(StaffSecurityOptions staff, string audience)
    {
        ArgumentNullException.ThrowIfNull(staff);
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = StaffSecurityOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = JwtStaffTokenIssuer.SigningKeyFor(staff),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = ClockSkew,
            RoleClaimType = JwtStaffTokenIssuer.RoleClaim,
        };
    }

    /// <summary>Reads the token of a call from one header; a missing or blank header leaves the call without a token.</summary>
    /// <param name="context">The context of the received message.</param>
    /// <param name="header">The name of the header that carries the token of the scheme.</param>
    public static void ReadToken(MessageReceivedContext context, string header)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(header);

        string token = context.Request.Headers[header].ToString().Trim();
        context.Token = token.Length == 0 ? null : token;
    }

    #endregion Public methods
}
