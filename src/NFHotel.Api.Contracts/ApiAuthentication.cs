namespace NFHotel.Api.Contracts;

/// <summary>How the web application proves to the API that it may call it (ADR-0004), and how a member of staff does (ADR-0005).</summary>
public static class ApiAuthentication
{
    /// <summary>The authorization scheme: <c>Authorization: Bearer &lt;token&gt;</c>.</summary>
    public const string Scheme = "Bearer";

    /// <summary>The configuration section of the API token on both sides.</summary>
    public const string SectionName = "Api";

    /// <summary>The key of the token in <see cref="SectionName"/>.</summary>
    public const string TokenName = "Token";

    /// <summary>The key of the API address in <see cref="SectionName"/>, read by the client.</summary>
    public const string BaseUrlName = "BaseUrl";

    /// <summary>The shortest token that is accepted; a shorter one is a setup mistake.</summary>
    public const int MinimumTokenLength = 32;

    /// <summary>
    /// The header with the token of the signed-in member of staff. The <c>Authorization</c> header is taken by the
    /// token of the web application, so the staff token travels beside it (ADR-0005).
    /// </summary>
    public const string StaffTokenHeader = "X-Staff-Token";

    /// <summary>
    /// The header with the token of a signed-in guest (MIL-018, ADR-0047). It travels beside the token of the web
    /// application, like the staff token, and is read only when the call carries no valid staff token.
    /// </summary>
    public const string GuestTokenHeader = "X-Guest-Token";

    /// <summary>
    /// The header with the address of the browser (MIL-087, ADR-0051 decision 4): nginx writes the address it saw into it
    /// and replaces a value that the caller wrote, the web application reads it from nginx and passes it on to the API,
    /// and the API reads it only on a call that carries the service token. The audit trail (ADR-0017) and the limits of
    /// the booking flow use it.
    /// </summary>
    public const string ClientAddressHeader = "X-NFHotel-Client-Address";
}
