namespace NFHotel.Infrastructure.Repositories;

/// <summary>
/// Builds <c>ILIKE</c> patterns with correct escaping.
/// </summary>
/// <remarks>
/// Without escaping a guest searching for "100%" or "a_b" would get arbitrary hits, because
/// <c>%</c> and <c>_</c> are wildcards in SQL. It is not a security fault — the value is
/// still a parameter — but it is a wrong result, and it would be hard to spot
/// in operation.
/// </remarks>
public static class SqlPattern
{
    /// <summary>The escape character sent along in <c>ILIKE ... ESCAPE</c>.</summary>
    public const string EscapeCharacter = "\\";

    /// <summary>
    /// Builds a "contains" pattern from free text.
    /// </summary>
    /// <param name="value">Free text from the user.</param>
    /// <returns>The pattern <c>%value%</c> with wildcards and escape characters escaped.</returns>
    public static string Contains(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var escaped = value
            .Replace(EscapeCharacter, EscapeCharacter + EscapeCharacter, StringComparison.Ordinal)
            .Replace("%", EscapeCharacter + "%", StringComparison.Ordinal)
            .Replace("_", EscapeCharacter + "_", StringComparison.Ordinal);

        return $"%{escaped}%";
    }
}
