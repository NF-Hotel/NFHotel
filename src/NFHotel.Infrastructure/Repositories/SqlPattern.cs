namespace NFHotel.Infrastructure.Repositories;

/// <summary>
/// Bygger <c>ILIKE</c>-mønstre med korrekt escaping.
/// </summary>
/// <remarks>
/// Uden escaping ville en gæst der søger efter "100%" eller "a_b" få vilkårlige træf, fordi
/// <c>%</c> og <c>_</c> er jokertegn i SQL. Det er ikke en sikkerhedsfejl — værdien er
/// stadig en parameter — men det er et forkert resultat, og det ville være svært at få øje
/// på i drift.
/// </remarks>
public static class SqlPattern
{
    /// <summary>Escape-tegnet der sendes med i <c>ILIKE ... ESCAPE</c>.</summary>
    public const string EscapeCharacter = "\\";

    /// <summary>
    /// Bygger et "indeholder"-mønster af en fritekst.
    /// </summary>
    /// <param name="value">Fritekst fra brugeren.</param>
    /// <returns>Mønsteret <c>%værdi%</c> med jokertegn og escape-tegn escaped.</returns>
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