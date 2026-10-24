namespace NFHotel.Web.Security;

/// <summary>
/// The antiforgery cookie follows the staff cookie (ADR-0006, MIL-033): behind the TLS proxy it is always
/// <c>Secure</c>, and it is <c>HttpOnly</c>. With the default <c>SameAsRequest</c> it was sent without <c>Secure</c>,
/// because the request reaches the application over HTTP behind the proxy (ZAP rule 10011).
/// </summary>
public static class AntiforgeryCookie
{
    /// <summary>Registers antiforgery with the cookie flags of the deployment.</summary>
    /// <param name="services">The services.</param>
    /// <param name="requireTls">Whether TLS is in front of the application (<c>Tls:Require</c>).</param>
    /// <returns>The services.</returns>
    public static IServiceCollection AddSecureAntiforgery(this IServiceCollection services, bool requireTls)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddAntiforgery(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = requireTls ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
        });
    }
}
