using System.Threading.RateLimiting;

using NFHotel.Api.Contracts;
using NFHotel.Api.Controllers;
using NFHotel.Application.Errors;

namespace NFHotel.Api.Security;

/// <summary>The limit on the writes that a Manager makes to staff accounts: 30 a minute per account (ADR-0015).</summary>
public static class StaffWriteRateLimit
{
    /// <summary>The number of writes that one account may make in a window.</summary>
    public const int PermitLimit = 30;

    /// <summary>Registers the policy <see cref="StaffController.WriteLimitPolicy"/>; a refused call gets 429 and the usual envelope.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddStaffWriteRateLimit(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(StaffController.WriteLimitPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.User.FindFirst("sub")?.Value ?? "anonymous",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = PermitLimit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new ApiResponse([ErrorCodes.Staff.TooManyRequests]),
                    ApiJson.Options,
                    cancellationToken).ConfigureAwait(false);
            };
        });
        return services;
    }
}
