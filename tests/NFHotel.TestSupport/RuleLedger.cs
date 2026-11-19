namespace NFHotel.TestSupport;

/// <summary>
/// Reads a table of the rule ledger, <c>docs/refactor/BusinessRules.md</c>, for the tests that hold it against the code:
/// the validation rules of MIL-086 and the limits of MIL-087.
/// </summary>
public static class RuleLedger
{
    /// <summary>Reads the rows of the first table of a section, in their order.</summary>
    /// <param name="section">The heading of the section, such as <c>## Limits of the booking flow</c>.</param>
    /// <param name="firstHeader">The text of the first cell of the header row, such as <c>Limit</c>.</param>
    /// <param name="columnCount">The number of cells of every row.</param>
    /// <returns>The trimmed cells of each row.</returns>
    /// <exception cref="InvalidOperationException">The ledger has no such section or table, or a row has another number of cells.</exception>
    public static IReadOnlyList<string[]> ReadTable(string section, string firstHeader, int columnCount)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(firstHeader);

        string[] lines = File.ReadAllLines(Path.Join(RepositoryRoot.Find(), "docs", "refactor", "BusinessRules.md"));
        int start = Array.IndexOf(lines, section);
        if (start < 0)
        {
            throw new InvalidOperationException($"The rule ledger has no section \"{section.TrimStart('#', ' ')}\".");
        }

        string headerStart = $"| {firstHeader} |";
        int header = Array.FindIndex(lines, start, line => line.StartsWith(headerStart, StringComparison.Ordinal));
        if (header < 0)
        {
            throw new InvalidOperationException("The section has no table.");
        }

        List<string[]> rows = [];
        for (int index = header + 2; index < lines.Length && lines[index].StartsWith('|'); index++)
        {
            string[] cells = [.. lines[index].Trim().Trim('|').Split('|').Select(cell => cell.Trim())];
            if (cells.Length != columnCount)
            {
                throw new InvalidOperationException($"Line {index + 1} of the rule ledger has {cells.Length} cells, not {columnCount}.");
            }

            rows.Add(cells);
        }

        return rows;
    }
}
