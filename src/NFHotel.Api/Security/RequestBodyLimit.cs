using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;

using NFHotel.Api.Contracts;

namespace NFHotel.Api.Security;

/// <summary>
/// The largest body of a request (MIL-086, ADR-0050 decision 4): <see cref="DefaultMaxBytes"/> at every endpoint, or
/// the limit that an endpoint sets with <c>RequestSizeLimit</c>. A body whose declared length is over the limit is
/// refused before it is read; a body that grows over it while it is read makes the server throw, and the handler of
/// exceptions answers the same way. Both answer 413 with <see cref="ApiErrorCodes.RequestTooLarge"/>.
/// </summary>
public static class RequestBodyLimit
{
    /// <summary>The most bytes of a request body at an endpoint that sets no limit of its own: 64 KB.</summary>
    public const long DefaultMaxBytes = 65_536;

    /// <summary>The most bytes of a run that the benchmark service sends to its intake.</summary>
    public const long BenchmarkIntakeMaxBytes = 30_000_000;

    /// <summary>Refuses a request whose declared body is over the limit of its endpoint, and sets that limit for the read.</summary>
    /// <param name="app">The application.</param>
    /// <returns>The application.</returns>
    public static IApplicationBuilder UseRequestBodyLimit(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var endpoint = context.GetEndpoint();
            var limit = endpoint?.Metadata.GetMetadata<IRequestSizeLimitMetadata>()?.MaxRequestBodySize ?? DefaultMaxBytes;

            if (context.Request.ContentLength > limit)
            {
                await RefuseAsync(context, SecurityEvents.RouteOf(endpoint)).ConfigureAwait(false);
                return;
            }

            if (context.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } feature)
            {
                feature.MaxRequestBodySize = limit;
            }

            await next(context).ConfigureAwait(false);
        });

    /// <summary>Answers 413 with the code of the API and writes the security event.</summary>
    /// <param name="context">The request.</param>
    /// <param name="route">The route template of its endpoint.</param>
    /// <returns>The work of the answer.</returns>
    public static async Task RefuseAsync(HttpContext context, string route)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(SecurityEvents.Category)
            .InputRefused(context.Request.Method, route, StatusCodes.Status413PayloadTooLarge, ApiErrorCodes.RequestTooLarge);

        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        await context.Response.WriteAsJsonAsync(
            new ApiResponse([ApiErrorCodes.RequestTooLarge]),
            ApiJson.Options,
            context.RequestAborted).ConfigureAwait(false);
    }
}
