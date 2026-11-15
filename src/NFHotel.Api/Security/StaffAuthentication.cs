using System.Globalization;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;

using NFHotel.Api.Contracts;
using NFHotel.Domain.Repositories;
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
                bearer.TokenValidationParameters = ApiTokenValidation.For(staff, StaffSecurityOptions.Audience);
                bearer.TokenValidationParameters.NameClaimType = JwtStaffTokenIssuer.NameClaim;
                bearer.Events = new JwtBearerEvents
                {
                    // A valid signature is not enough: the account must still exist, be active and have the stamp that the
                    // token was issued with, so a password change, a reset or a disable ends the old sessions at once (ADR-0017).
                    OnTokenValidated = async context =>
                    {
                        var principal = context.Principal;
                        if (!int.TryParse(principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
                            || !int.TryParse(principal?.FindFirst(JwtStaffTokenIssuer.StampClaim)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stamp))
                        {
                            context.Fail("The token has no account or stamp.");
                            return;
                        }

                        var accounts = context.HttpContext.RequestServices.GetRequiredService<IStaffRepository>();
                        var member = await accounts.GetByIdAsync(id, context.HttpContext.RequestAborted).ConfigureAwait(false);
                        if (member is null || !member.IsActive || member.SecurityStamp != stamp)
                        {
                            context.Fail("The account changed after the token was issued.");
                        }
                    },

                    // The Authorization header belongs to the token of the web application (ADR-0004).
                    OnMessageReceived = context =>
                    {
                        ApiTokenValidation.ReadToken(context, ApiAuthentication.StaffTokenHeader);
                        return Task.CompletedTask;
                    },
                };
            });
        return services;
    }
}
