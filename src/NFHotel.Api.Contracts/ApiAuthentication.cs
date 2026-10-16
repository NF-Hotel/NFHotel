namespace NFHotel.Api.Contracts;

/// <summary>How the web application proves to the API that it may call it (ADR-0004).</summary>
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
}