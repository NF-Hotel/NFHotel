using System.Text.RegularExpressions;

namespace NFHotel.Application.Tests.Architecture;

/// <summary>
/// Enforces where asynchronous code is used (MIL-014): the code waits for the database, an HTTP service or a
/// file without blocking a thread, the domain stays synchronous because it only computes, and every
/// asynchronous call can be cancelled. The rules are read from the source of all four layers.
/// </summary>
public sealed partial class AsyncRulesTests
{
    #region Blocking calls

    /// <summary>No layer waits for a task by blocking: no <c>.Result</c>, <c>.Wait()</c>, <c>GetAwaiter().GetResult()</c>, <c>Task.Run</c> or <c>async void</c>.</summary>
    [Fact]
    public void Source_InEveryLayer_HasNoBlockingCallOrAsyncVoid()
    {
        // Arrange
        Assert.True(SourceLines().Count() > 1000, "The scan found too few source lines to mean anything.");
        var offenders = SourceLines()
            .Where(line => BlockingCall().IsMatch(line.Code))
            .Select(line => line.Where)
            .ToArray();

        // Act and assert
        Assert.True(offenders.Length == 0, $"Blocking calls or async void: {string.Join(", ", offenders)}");
    }

    #endregion

    #region Naming

    /// <summary>Every method that returns a task ends in <c>Async</c>.</summary>
    [Fact]
    public void TaskReturningMethods_InEveryLayer_EndInAsync()
    {
        // Arrange
        Assert.True(SourceFiles().SelectMany(file => TaskMethod().Matches(Code(file))).Count() > 50, "The scan found too few asynchronous methods to mean anything.");
        var offenders = SourceFiles()
            .SelectMany(file => TaskMethod().Matches(Code(file)).Select(match => (file, name: match.Groups["name"].Value)))
            .Where(method => !method.name.EndsWith("Async", StringComparison.Ordinal))
            .Select(method => $"{Path.GetFileName(method.file)}: {method.name}")
            .ToArray();

        // Act and assert
        Assert.True(offenders.Length == 0, $"Task-returning methods without the Async suffix: {string.Join(", ", offenders)}");
    }

    #endregion

    #region The domain computes

    /// <summary>Entities, value objects and errors of the domain are synchronous: they wait for nothing.</summary>
    [Fact]
    public void DomainTypes_ThatAreNotRepositories_DoNotUseTasks()
    {
        // Arrange
        var offenders = SourceFiles("NFHotel.Domain")
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}Repositories{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => DomainAsync().IsMatch(Code(file)))
            .Select(Path.GetFileName)
            .ToArray();

        // Act and assert
        Assert.True(offenders.Length == 0, $"Domain files that use async or Task: {string.Join(", ", offenders)}");
    }

    #endregion

    #region Cancellation

    /// <summary>Every public asynchronous method of the application, the infrastructure, the API, its client and the external adapters can be cancelled.</summary>
    [Fact]
    public void PublicAsyncMethods_OfApplicationAndInfrastructure_TakeACancellationToken()
    {
        // Arrange
        var all = SourceFiles("NFHotel.Application", "NFHotel.Infrastructure", "NFHotel.Api", "NFHotel.ApiClient", "NFHotel.External")
            .SelectMany(file => PublicTaskMethod().Matches(Code(file)))
            .ToArray();
        Assert.True(all.Length > 30, "The scan found too few public asynchronous methods to mean anything.");
        var offenders = SourceFiles("NFHotel.Application", "NFHotel.Infrastructure", "NFHotel.Api", "NFHotel.ApiClient", "NFHotel.External")
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .SelectMany(file => PublicTaskMethod().Matches(Code(file)).Select(match => (file, name: match.Groups["name"].Value, parameters: match.Groups["parameters"].Value)))
            // A middleware receives the HttpContext, whose RequestAborted is the token of the call.
            .Where(method => !method.parameters.Contains("CancellationToken", StringComparison.Ordinal)
                && !method.parameters.Contains("HttpContext", StringComparison.Ordinal))
            .Select(method => $"{Path.GetFileName(method.file)}: {method.name}")
            .ToArray();

        // Act and assert
        Assert.True(offenders.Length == 0, $"Public asynchronous methods without a CancellationToken: {string.Join(", ", offenders)}");
    }

    /// <summary>A page passes the token of the component to every call of its view model, so the call stops when the page goes away.</summary>
    [Fact]
    public void ViewModelCalls_InEveryComponent_PassTheComponentToken()
    {
        // Arrange
        var components = SourceFiles("NFHotel.Web").Where(file => file.EndsWith(".razor", StringComparison.Ordinal)).ToArray();
        var calls = components.SelectMany(file => ViewModelCall().Matches(Code(file)).Select(match => (file, call: match.Value))).ToArray();
        Assert.True(calls.Length > 20, "The scan found too few view-model calls to mean anything.");
        var offenders = calls
            .Where(call => !call.call.Contains("ComponentToken", StringComparison.Ordinal))
            .Select(call => $"{Path.GetFileName(call.file)}: {call.call}")
            .ToArray();

        // Act and assert
        Assert.True(offenders.Length == 0, $"View-model calls without the component token: {string.Join(", ", offenders)}");
    }

    #endregion

    #region Helpers

    private static IEnumerable<string> SourceFiles(params string[] projects)
    {
        var src = Path.Combine(RepositoryRoot.Find(), "src");
        var selected = projects.Length == 0
            ? Directory.EnumerateDirectories(src)
            : projects.Select(project => Path.Combine(src, project));

        return selected
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories))
            .Where(file => file.EndsWith(".cs", StringComparison.Ordinal) || file.EndsWith(".razor", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    private static IEnumerable<(string Where, string Code)> SourceLines() =>
        SourceFiles().SelectMany(file => File.ReadAllLines(file)
            .Select((line, index) => (Where: $"{Path.GetFileName(file)}:{index + 1}", Code: CodeOnly(line))));

    private static string Code(string file) => string.Join('\n', File.ReadAllLines(file).Select(CodeOnly));

    private static string CodeOnly(string line)
    {
        var comment = line.IndexOf("//", StringComparison.Ordinal);
        return comment < 0 ? line : line[..comment];
    }

    [GeneratedRegex(@"\.Result\b|\.Wait\s*\(|GetAwaiter\s*\(\s*\)\s*\.GetResult|Task\.Run\s*\(|async\s+void\b")]
    private static partial Regex BlockingCall();

    [GeneratedRegex(@"(?:^|\s)(?:public|private|internal|protected)\s+(?:static\s+|override\s+|virtual\s+|sealed\s+)*(?:async\s+)?(?:Task|ValueTask)(?:<(?:[^<>]|<[^<>]*>)*>)?\s+(?<name>\w+)\s*\(", RegexOptions.Multiline)]
    private static partial Regex TaskMethod();

    [GeneratedRegex(@"(?:^|\s)public\s+(?:static\s+|override\s+|virtual\s+)*(?:async\s+)?(?:Task|ValueTask)(?:<(?:[^<>]|<[^<>]*>)*>)?\s+(?<name>\w+)\s*\((?<parameters>[^)]*)\)", RegexOptions.Multiline)]
    private static partial Regex PublicTaskMethod();

    [GeneratedRegex(@"\b(?:ViewModel|Form)\.\w+Async\([^()]*\)")]
    private static partial Regex ViewModelCall();

    [GeneratedRegex(@"\basync\b|\bTask\b|\bValueTask\b")]
    private static partial Regex DomainAsync();

    #endregion
}