namespace NFHotel.Application.Tests.Architecture;

/// <summary>
/// Keeps the database behind the API (ADR-0004): the projects that run in or serve the internet-facing
/// application carry no reference to the persistence code, to EF Core or to Npgsql, neither directly nor through
/// another package. The check reads the project files and the restore result of each project.
/// </summary>
public sealed class DatabaseIsolationTests
{
    private static readonly string[] Forbidden =
    [
        "NFHotel.Infrastructure",
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
        "EFCore.NamingConventions",
    ];

    private static readonly string[] WebSideProjects =
    [
        "NFHotel.Web",
        "NFHotel.ApiClient",
        "NFHotel.External",
        "NFHotel.Api.Contracts",
    ];

    #region Project files

    /// <summary>The project files of the web side name no persistence project or package.</summary>
    [Fact]
    public void ProjectFiles_OfTheWebSide_NameNoDatabaseCode()
    {
        // Arrange
        var offenders = WebSideProjects
            .SelectMany(project => Forbidden
                .Where(name => File.ReadAllText(ProjectFile(project)).Contains($"Include=\"{name}", StringComparison.Ordinal)
                    || File.ReadAllText(ProjectFile(project)).Contains($"{name}.csproj", StringComparison.Ordinal))
                .Select(name => $"{project} -> {name}"))
            .ToArray();

        // Act and assert
        Assert.True(offenders.Length == 0, $"References to database code: {string.Join(", ", offenders)}");
    }

    #endregion

    #region Restore result

    /// <summary>The restored dependency graph of the web side, including every transitive package, holds no database code.</summary>
    [Fact]
    public void RestoredGraph_OfTheWebSide_HoldsNoDatabaseCode()
    {
        // Arrange
        var offenders = new List<string>();
        foreach (var project in WebSideProjects)
        {
            var assets = AssetsFile(project);
            Assert.True(File.Exists(assets), $"{assets} was not found; build the solution first.");
            var text = File.ReadAllText(assets);
            offenders.AddRange(Forbidden
                .Where(name => text.Contains($"\"{name}", StringComparison.Ordinal))
                .Select(name => $"{project} -> {name}"));
        }

        // Act and assert
        Assert.True(offenders.Count == 0, $"Database code in the dependency graph: {string.Join(", ", offenders)}");
    }

    /// <summary>The check can see a project that does reference the database code, so a pass means something.</summary>
    [Fact]
    public void RestoredGraph_OfTheApi_HoldsTheDatabaseCode()
    {
        // Arrange
        var text = File.ReadAllText(AssetsFile("NFHotel.Api"));

        // Act and assert
        Assert.Contains("\"Npgsql", text, StringComparison.Ordinal);
        Assert.Contains("\"NFHotel.Infrastructure", text, StringComparison.Ordinal);
    }

    #endregion

    #region Helpers

    private static string ProjectFile(string project) =>
        Path.Combine(RepositoryRoot.Find(), "src", project, $"{project}.csproj");

    private static string AssetsFile(string project) =>
        Path.Combine(RepositoryRoot.Find(), "g", "obj", project, "project.assets.json");

    #endregion
}
