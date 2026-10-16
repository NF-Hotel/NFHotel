using System.Globalization;

namespace NFHotel.Web.Features.Admin;

/// <summary>
/// Formatering af datoer og tidspunkter i admin-tabeller.
/// </summary>
/// <remarks>
/// UI-Spec afsnit 4: datoer skrives <c>dd MMM yyyy</c> i tabeller. Formatet står ét sted,
/// så en kolonne ikke kan komme til at skrive datoen anderledes end nabokolonnen.
/// <para>
/// <see cref="CultureInfo.InvariantCulture"/> bruges bevidst: månedsforkortelsen skal være
/// den samme engelske uanset hvilken kultur serveren tilfældigvis kører med, og et rent
/// talformat som 01.02.2026 læses forskelligt af danske og amerikanske gæster.
/// </para>
/// </remarks>
public static class AdminFormat
{
    /// <summary>Det format datoer skrives med i admin (UI-Spec afsnit 4).</summary>
    private const string DatePattern = "dd MMM yyyy";

    /// <summary>Format til et klokkeslæt uden dato.</summary>
    private const string TimePattern = "HH:mm";

    /// <summary>Teksten der står hvor en valgfri dato ikke er sat.</summary>
    private const string Missing = "—";

    /// <summary>
    /// Skriver en dato som <c>dd MMM yyyy</c>.
    /// </summary>
    /// <param name="value">Datoen.</param>
    /// <returns>Den formaterede dato.</returns>
    public static string Date(DateOnly value) =>
        value.ToString(DatePattern, CultureInfo.InvariantCulture);

    /// <summary>
    /// Skriver en valgfri dato som <c>dd MMM yyyy</c>.
    /// </summary>
    /// <param name="value">Datoen, eller <c>null</c>.</param>
    /// <returns>Den formaterede dato, eller en tankestreg.</returns>
    public static string Date(DateOnly? value) => value is null ? Missing : Date(value.Value);

    /// <summary>
    /// Skriver et tidspunkt som <c>dd MMM yyyy HH:mm</c>.
    /// </summary>
    /// <param name="value">Tidspunktet, eller <c>null</c>.</param>
    /// <returns>Det formaterede tidspunkt, eller en tankestreg.</returns>
    public static string DateAndTime(DateTimeOffset? value) => value is null
        ? Missing
        : value.Value.ToString($"{DatePattern} {TimePattern}", CultureInfo.InvariantCulture);
}