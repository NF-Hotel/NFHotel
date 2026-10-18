using System.Text.RegularExpressions;

namespace NFHotel.Application.Tests.Architecture;

/// <summary>
/// Enforces the test rules of the coding conventions on every test project (MIL-012): three-part
/// test names, no sleeping or system clock in a test, and named regions in long test classes.
/// </summary>
public sealed partial class TestRulesTests
{
    private const int LongClassLines = 150;

    #region Naming

    /// <summary>Every test is named <c>Method_Condition_Expected</c>: at least three parts.</summary>
    [Fact]
    public void TestMethods_InEveryTestProject_HaveThreePartNames()
    {
        var offenders = TestFiles()
            .SelectMany(file => TestMethodName().Matches(File.ReadAllText(file)).Select(match => (file, name: match.Groups["name"].Value)))
            .Where(test => test.name.Split('_', StringSplitOptions.RemoveEmptyEntries).Length < 3)
            .Select(test => $"{Path.GetFileName(test.file)}: {test.name}")
            .ToArray();

        Assert.True(offenders.Length == 0, $"Test names with fewer than three parts: {string.Join(", ", offenders)}");
    }

    #endregion

    #region Determinism

    /// <summary>A test neither sleeps nor reads the system clock: it waits on a latch and gets "today" from outside.</summary>
    [Fact]
    public void Tests_InEveryTestProject_DoNotSleepOrReadTheSystemClock()
    {
        var offenders = TestFiles()
            .Where(file => !file.EndsWith(nameof(TestRulesTests) + ".cs", StringComparison.Ordinal))
            .SelectMany(file => File.ReadAllLines(file)
                .Select((line, index) => (file, line: CodeOnly(line), number: index + 1)))
            .Where(entry => ForbiddenCall().IsMatch(entry.line))
            .Select(entry => $"{Path.GetFileName(entry.file)}:{entry.number}")
            .ToArray();

        Assert.True(offenders.Length == 0, $"Tests that sleep or read the system clock: {string.Join(", ", offenders)}");
    }

    #endregion

    #region Regions

    /// <summary>A test class of more than 150 lines has at least two named regions, each closed.</summary>
    [Fact]
    public void LongTestClasses_InEveryTestProject_HaveNamedRegions()
    {
        var offenders = new List<string>();
        foreach (var file in TestFiles().Where(file => file.EndsWith("Tests.cs", StringComparison.Ordinal)))
        {
            var lines = File.ReadAllLines(file);
            if (lines.Length <= LongClassLines)
            {
                continue;
            }

            var starts = lines.Count(line => NamedRegion().IsMatch(line));
            var ends = lines.Count(line => line.TrimStart().StartsWith("#endregion", StringComparison.Ordinal));
            if (starts < 2 || starts != ends)
            {
                offenders.Add($"{Path.GetFileName(file)} ({starts} regions, {ends} ends)");
            }
        }

        Assert.True(offenders.Count == 0, $"Long test classes without matching named regions: {string.Join(", ", offenders)}");
    }

    #endregion

    #region Helpers

    private static IEnumerable<string> TestFiles() =>
        Directory.EnumerateFiles(Path.Combine(RepositoryRoot.Find(), "tests"), "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    private static string CodeOnly(string line)
    {
        var comment = line.IndexOf("//", StringComparison.Ordinal);
        return comment < 0 ? line : line[..comment];
    }

    [GeneratedRegex(@"\[(?:Fact|Theory|LiveFact)[^\]]*\]\s*(?:\[[^\]]*\]\s*)*public\s+(?:async\s+)?(?:Task|ValueTask|void)\s+(?<name>\w+)\s*\(", RegexOptions.Singleline)]
    private static partial Regex TestMethodName();

    [GeneratedRegex(@"Thread\.Sleep\s*\(|Task\.Delay\s*\(|(?:DateTime|DateTimeOffset)\.(?:Now|UtcNow)\b")]
    private static partial Regex ForbiddenCall();

    [GeneratedRegex(@"^\s*#region\s+\S")]
    private static partial Regex NamedRegion();

    #endregion
}
