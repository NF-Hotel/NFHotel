namespace NFHotel.Web.Security;

/// <summary>Checks the address that the sign-in returns to, so the login page cannot be used to send a user to another site.</summary>
public static class ReturnUrl
{
    /// <summary>Accepts a path of this site below <c>/admin</c> and refuses everything else.</summary>
    /// <param name="candidate">The <c>returnUrl</c> of the query string.</param>
    /// <param name="fallback">The page to use when the candidate is not accepted.</param>
    /// <returns>The candidate, or the fallback.</returns>
    public static string Safe(string? candidate, string fallback)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fallback);

        if (string.IsNullOrWhiteSpace(candidate)
            || !candidate.StartsWith("/admin", StringComparison.OrdinalIgnoreCase)
            || candidate.StartsWith("/admin/login", StringComparison.OrdinalIgnoreCase)
            || candidate.Contains("//", StringComparison.Ordinal)
            || candidate.Contains('\\')
            || candidate.Any(char.IsControl))
        {
            return fallback;
        }

        return candidate.Length == "/admin".Length || candidate["/admin".Length] is '/' or '?' ? candidate : fallback;
    }
}