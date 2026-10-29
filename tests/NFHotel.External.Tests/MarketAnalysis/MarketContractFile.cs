namespace NFHotel.External.Tests.MarketAnalysis;

/// <summary>
/// A small reader of <c>openapi/market-contract.yaml</c> of the market analysis service. The test project holds a byte-identical
/// copy (<c>docs/test-data/market-contract.yaml</c>, copied to <c>TestData/</c>); the fake of the service takes the paths and the
/// fields of the booking from it, so the fake cannot drift from the contract. The file is read as lines, because the contract
/// is regular (two-space indentation) and a YAML package would only be used here.
/// </summary>
internal sealed class MarketContractFile
{
    private const string SchemasHeader = "  schemas:";
    private readonly string[] _lines;

    private MarketContractFile(string[] lines) => _lines = lines;

    /// <summary>Reads the copy of the contract that is next to the test assembly.</summary>
    public static MarketContractFile Load() =>
        new(File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "TestData", "market-contract.yaml")));

    /// <summary>The paths of the contract, for example <c>/api/v1/market-analysis/runs</c>.</summary>
    public IReadOnlyList<string> Paths()
    {
        var start = Array.FindIndex(_lines, line => line == "paths:");
        var end = Array.FindIndex(_lines, start + 1, line => line.Length > 0 && line[0] != ' ');

        return [.. _lines[(start + 1)..end].Where(line => line.StartsWith("  /", StringComparison.Ordinal) && line.EndsWith(':')).Select(line => line.Trim().TrimEnd(':'))];
    }

    /// <summary>The names in the <c>required</c> list of a schema of <c>components/schemas</c>.</summary>
    public IReadOnlyList<string> Required(string schema)
    {
        var block = Schema(schema);
        var index = Array.FindIndex(block, line => line.StartsWith("      required:", StringComparison.Ordinal));
        if (index < 0)
        {
            return [];
        }

        var inline = block[index]["      required:".Length..].Trim();
        if (inline.StartsWith('['))
        {
            return [.. inline.Trim('[', ']').Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];
        }

        return [.. block[(index + 1)..].TakeWhile(line => line.StartsWith("        - ", StringComparison.Ordinal)).Select(line => line.Trim()[2..].Trim())];
    }

    /// <summary>The names of the <c>properties</c> of a schema of <c>components/schemas</c>.</summary>
    public IReadOnlyList<string> Properties(string schema)
    {
        var block = Schema(schema);
        var index = Array.FindIndex(block, line => line == "      properties:");

        return [.. block[(index + 1)..]
            .TakeWhile(line => line.StartsWith("        ", StringComparison.Ordinal))
            .Where(line => line[8] != ' ' && line[8] != '-')
            .Select(line => line.Trim()[..line.Trim().IndexOf(':', StringComparison.Ordinal)])];
    }

    /// <summary>The names in the <c>required</c> list of an object property of a schema, for example <c>analyses</c> of <c>RunResult</c>.</summary>
    public IReadOnlyList<string> NestedRequired(string schema, string property)
    {
        var block = Schema(schema);
        var start = Array.FindIndex(block, line => line == $"        {property}:");
        var index = Array.FindIndex(block, start + 1, line => line.StartsWith("          required:", StringComparison.Ordinal));

        return [.. block[(index + 1)..].TakeWhile(line => line.StartsWith("            - ", StringComparison.Ordinal)).Select(line => line.Trim()[2..].Trim())];
    }

    private string[] Schema(string name)
    {
        var start = Array.FindIndex(_lines, line => line == SchemasHeader);
        var header = Array.FindIndex(_lines, start + 1, line => line == $"    {name}:");
        var rest = _lines[(header + 1)..];
        var length = Array.FindIndex(rest, line => line.Length > 0 && !line.StartsWith("     ", StringComparison.Ordinal));

        return length < 0 ? rest : rest[..length];
    }
}
