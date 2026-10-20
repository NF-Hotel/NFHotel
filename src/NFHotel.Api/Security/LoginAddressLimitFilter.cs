using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Staff;
using NFHotel.Application.Errors;

namespace NFHotel.Api.Security;

/// <summary>
/// Limits the sign-in per client address for a call that arrives without the token of the web application, which is
/// the mobile app (ADR-0015). It keeps the controller thin: it refuses with 429 when the address has used up its
/// attempts, and counts a refusal for wrong credentials (401) after the sign-in has run.
/// </summary>
public sealed class LoginAddressLimitFilter : IAsyncActionFilter
{
    private readonly AddressLoginThrottle _addresses;

    /// <summary>Creates the filter.</summary>
    /// <param name="addresses">Counts failed sign-ins per client address.</param>
    public LoginAddressLimitFilter(AddressLoginThrottle addresses)
    {
        ArgumentNullException.ThrowIfNull(addresses);
        _addresses = addresses;
    }

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (!StaffTokenRoutes.HasNoServiceToken(context.HttpContext.Request))
        {
            await next().ConfigureAwait(false);
            return;
        }

        var address = context.HttpContext.Connection.RemoteIpAddress;
        if (_addresses.IsBlocked(address))
        {
            context.Result = new ObjectResult(new ApiResponse<StaffSessionDto>(default, [ErrorCodes.Staff.TooManyAttempts]))
            {
                StatusCode = StatusCodes.Status429TooManyRequests,
            };
            return;
        }

        var executed = await next().ConfigureAwait(false);
        if (executed is { Result: ObjectResult { StatusCode: StatusCodes.Status401Unauthorized } })
        {
            _addresses.RecordFailure(address);
        }
    }
}
