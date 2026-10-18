using System.Security.Cryptography;
using System.Text;

using NFHotel.Api.Contracts;

namespace NFHotel.Api.Security;

/// <summary>
/// Lets a call to <c>/api</c> through only with the bearer token of the web application (ADR-0004). The
/// comparison takes the same time whatever the first wrong character is. The health endpoint is open.
/// </summary>
public sealed class ApiTokenMiddleware
{
    private const string ApiPrefix = "/api";

    private readonly RequestDelegate _next;
    private readonly byte[] _expected;

    /// <summary>Creates the middleware.</summary>
    /// <param name="next">The next step of the pipeline.</param>
    /// <param name="token">The token that callers must present.</param>
    public ApiTokenMiddleware(RequestDelegate next, ApiToken token)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(token);

        _next = next;
        _expected = Encoding.UTF8.GetBytes(token.Value);
    }

    /// <summary>Checks the token of an <c>/api</c> call and answers 401 when it is missing or wrong.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that completes when the call has been answered.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Request.Path.StartsWithSegments(ApiPrefix, StringComparison.OrdinalIgnoreCase) && !IsAuthorized(context))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = ApiAuthentication.Scheme;
            await context.Response
                .WriteAsJsonAsync(new ApiResponse(["api.unauthorized"]), ApiJson.Options, context.RequestAborted)
                .ConfigureAwait(false);
            return;
        }

        await _next(context).ConfigureAwait(false);
    }

    private bool IsAuthorized(HttpContext context)
    {
        var header = context.Request.Headers.Authorization.ToString();
        var prefix = ApiAuthentication.Scheme + " ";
        if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var presented = Encoding.UTF8.GetBytes(header[prefix.Length..].Trim());
        return CryptographicOperations.FixedTimeEquals(presented, _expected);
    }
}