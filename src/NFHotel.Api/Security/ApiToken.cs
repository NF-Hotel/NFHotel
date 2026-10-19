using NFHotel.Api.Contracts;

namespace NFHotel.Api.Security;

/// <summary>The token that callers of the API must present, read from the configuration at start-up.</summary>
/// <param name="Value">The token.</param>
public sealed record ApiToken(string Value)
{
    /// <summary>Reads the token from the configuration and refuses a missing or short one.</summary>
    /// <param name="configuration">The configuration with the section <c>Api</c>.</param>
    /// <returns>The token.</returns>
    public static ApiToken Read(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var value = configuration.GetSection(ApiAuthentication.SectionName)[ApiAuthentication.TokenName]?.Trim() ?? string.Empty;
        if (value.Length < ApiAuthentication.MinimumTokenLength)
        {
            throw new InvalidOperationException(
                $"'{ApiAuthentication.SectionName}:{ApiAuthentication.TokenName}' must be at least {ApiAuthentication.MinimumTokenLength} characters. Create one with: openssl rand -base64 32");
        }

        return new ApiToken(value);
    }
}
