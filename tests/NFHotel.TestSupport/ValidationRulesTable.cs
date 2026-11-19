using System.Reflection;
using System.Text.RegularExpressions;

namespace NFHotel.TestSupport;

/// <summary>
/// Reads the table "Validation rules of the booking flow" of the rule ledger, <c>docs/refactor/BusinessRules.md</c>
/// (MIL-086 decision 1, ADR-0050), for the tests that hold it against the code of the API and of the web application.
/// </summary>
public static partial class ValidationRulesTable
{
    /// <summary>The heading of the section that holds the table.</summary>
    public const string Section = "## Validation rules of the booking flow";

    /// <summary>The start of the field of a row that is a value of the address of a page, such as <c>/book?guests</c>.</summary>
    public const string PageRowPrefix = "/";

    private const int ColumnCount = 10;

    /// <summary>Reads the rows of the table, in their order.</summary>
    /// <returns>The rows.</returns>
    /// <exception cref="InvalidOperationException">The ledger has no such section or table, or a row has another number of cells.</exception>
    public static IReadOnlyList<ValidationRule> Read()
    {
        string[] lines = File.ReadAllLines(Path.Join(RepositoryRoot.Find(), "docs", "refactor", "BusinessRules.md"));
        int start = Array.IndexOf(lines, Section);
        if (start < 0)
        {
            throw new InvalidOperationException($"The rule ledger has no section \"{Section[3..]}\".");
        }

        int header = Array.FindIndex(lines, start, line => line.StartsWith("| Field |", StringComparison.Ordinal));
        if (header < 0)
        {
            throw new InvalidOperationException("The section has no table.");
        }

        List<ValidationRule> rows = [];
        for (int index = header + 2; index < lines.Length && lines[index].StartsWith('|'); index++)
        {
            string[] cells = lines[index].Trim().Trim('|').Split('|').Select(cell => cell.Trim()).ToArray();
            if (cells.Length != ColumnCount)
            {
                throw new InvalidOperationException($"Line {index + 1} of the rule ledger has {cells.Length} cells, not {ColumnCount}.");
            }

            rows.Add(new ValidationRule(cells));
        }

        return rows;
    }

    /// <summary>The names of code in back quotes in a cell, such as <c>Booking.CheckPeriod</c> or <c>DateRange</c>.</summary>
    /// <param name="cell">The text of a cell.</param>
    /// <returns>The names, in their order.</returns>
    public static IEnumerable<string> NamesOfCode(string cell) =>
        CodeSpanPattern().Matches(cell).Select(match => match.Groups[1].Value);

    /// <summary>Tells whether a name of the last column exists: a type, or a type and a member of it, public or not.</summary>
    /// <param name="name">The name, such as <c>DateRange</c> or <c>Booking.CheckPeriod</c>.</param>
    /// <param name="assemblies">The assemblies whose types the name may name.</param>
    /// <returns><see langword="true"/> when the type, and the member if one is named, exist.</returns>
    public static bool Exists(string name, IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(name);
        string[] parts = name.Split('.');
        Type[] types = assemblies.SelectMany(assembly => assembly.GetTypes()).Where(type => type.Name == parts[0]).ToArray();
        return parts.Length switch
        {
            1 => types.Length > 0,
            2 => types.Any(type => type.GetMember(parts[1], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).Length > 0),
            _ => false,
        };
    }

    // A name of code in back quotes, such as `Booking.CheckPeriod` or `DateRange`.
    [GeneratedRegex("`([A-Z][A-Za-z0-9]*(?:\\.[A-Z][A-Za-z0-9]*)?)`", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex CodeSpanPattern();
}

/// <summary>One row of the table of validation rules.</summary>
/// <param name="Cells">The ten cells of the row, trimmed.</param>
public sealed record ValidationRule(IReadOnlyList<string> Cells)
{
    /// <summary>The field, without its back quotes: <c>GuestFields.Email</c> or <c>/book?guests</c>.</summary>
    public string Field => Cells[0].Trim('`');

    /// <summary>The type of the value: text, date, whole number, boolean, an enum or an object.</summary>
    public string Type => Cells[1];

    /// <summary>The length or the range of the value.</summary>
    public string Length => Cells[3];

    /// <summary>The codes that a refusal answers.</summary>
    public string Code => Cells[7];

    /// <summary>The types and methods that hold the rule.</summary>
    public string RuleInTheCode => Cells[9];
}
