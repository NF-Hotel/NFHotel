using NFHotel.Api.Contracts;

namespace NFHotel.Api.Security;

/// <summary>
/// The routes that the mobile app may call without the token of the web application (ADR-0015): the sign-in, which has
/// no staff token yet, and the rooms, which need a staff token that the permission of each action then validates. A
/// phone cannot keep a shared secret, so every other route still needs the service token. This is the one list.
/// </summary>
public static class StaffTokenRoutes
{
    private const string LoginPath = "/" + ApiRoutes.Auth + "/" + ApiRoutes.Login;
    private const string RoomsPath = "/" + ApiRoutes.Rooms;

    /// <summary>Tells whether a call sends no bearer token at all, which is how the mobile app calls (the one place that says so).</summary>
    /// <param name="request">The call.</param>
    /// <returns>True when there is no <c>Authorization</c> header.</returns>
    public static bool HasNoServiceToken(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Headers.Authorization.Count == 0;
    }

    /// <summary>Tells whether a call without the service token may go on.</summary>
    /// <param name="request">The call.</param>
    /// <returns>True for the sign-in, and for a rooms route that carries a staff token header.</returns>
    public static bool Accepts(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Path.Equals(LoginPath, StringComparison.OrdinalIgnoreCase))
        {
            return HttpMethods.IsPost(request.Method);
        }

        return request.Path.StartsWithSegments(RoomsPath, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(request.Headers[ApiAuthentication.StaffTokenHeader].ToString());
    }
}
