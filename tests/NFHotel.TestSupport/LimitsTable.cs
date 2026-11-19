using System.Globalization;

namespace NFHotel.TestSupport;

/// <summary>
/// Reads the table "Limits of the booking flow" of the rule ledger, <c>docs/refactor/BusinessRules.md</c> (MIL-087
/// decision 1, ADR-0051), for the test that holds it against the settings and the windows of the API.
/// </summary>
public static class LimitsTable
{
    /// <summary>The heading of the section that holds the table.</summary>
    public const string Section = "## Limits of the booking flow";

    private const int ColumnCount = 7;

    /// <summary>Reads the rows of the table, in their order.</summary>
    /// <returns>The rows.</returns>
    public static IReadOnlyList<LimitRule> Read() =>
        [.. RuleLedger.ReadTable(Section, "Limit", ColumnCount).Select(cells => new LimitRule(cells))];
}

/// <summary>One row of the table of the limits.</summary>
/// <param name="Cells">The seven cells of the row, trimmed.</param>
public sealed record LimitRule(IReadOnlyList<string> Cells)
{
    /// <summary>The name of the limit, without its back quotes, as the security events name it.</summary>
    public string Limit => Cells[0].Trim('`');

    /// <summary>The window, such as <c>1 minute</c>, or <c>None</c>.</summary>
    public string Window => Cells[3];

    /// <summary>The number of the limit.</summary>
    public int Number => int.Parse(Cells[4], NumberStyles.Integer, CultureInfo.InvariantCulture);

    /// <summary>The setting that holds the number, without its back quotes, such as <c>SenderLimits:SearchesPerMinute</c>.</summary>
    public string Setting => Cells[5].Trim('`');

    /// <summary>The status, the code and the header of the refusal.</summary>
    public string Refusal => Cells[6];
}
