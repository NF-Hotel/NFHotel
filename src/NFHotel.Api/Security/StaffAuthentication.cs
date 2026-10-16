using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using NFHotel.Api.Contracts;
using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Security;

namespace NFHotel.Api.Security;

/// <summary>Validation of the staff token that arrives in <see cref="ApiAuthentication.StaffTokenHeader"/> (ADR-0005).</summary>
public static class StaffAuthentication
{
    /// <summary>Registers the JWT validation. A token that is missing, tampered, expired or signed with another key leaves the user anonymous.</summary>
    /// <param name="services">The service collection; <see cref="StaffSecurityOptions"/> must be registered.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddStaffAuthentication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<StaffSecurityOptions>((bearer, staff) =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = StaffSecurityOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = StaffSecurityOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = JwtStaffTokenIssuer.SigningKeyFor(staff),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtStaffTokenIssuer.NameClaim,
                    RoleClaimType = JwtStaffTokenIssuer.RoleClaim,
                };
                bearer.Events = new JwtBearerEvents
                {
                    // The Authorization header belongs to the token of the web application (ADR-0004).
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Headers[ApiAuthentication.StaffTokenHeader].ToString().Trim();
                        context.Token = token.Length == 0 ? null : token;
                        return Task.CompletedTask;
                    },
                };
            });
        return services;
    }
}