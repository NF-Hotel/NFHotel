namespace NFHotel.ApiClient;

/// <summary>
/// Carries the staff token of one use-case call from the web application to the gateway (ADR-0005). It is scoped:
/// the runner of the web application makes one scope per call, puts the token of the signed-in user in it, and the
/// gateway of that scope sends it in the staff header. A call of the public booking pages has no token.
/// </summary>
public sealed class StaffCallContext
{
    /// <summary>The signed token of the member of staff, or <c>null</c> when nobody is signed in.</summary>
    public string? Token { get; set; }

    /// <summary>
    /// The address of the browser, passed on to the API in <c>X-Forwarded-For</c> for the audit trail (ADR-0017), or
    /// <c>null</c> when it is unknown. It is only as trustworthy as the proxy that the browser came through.
    /// </summary>
    public string? ClientAddress { get; set; }
}
