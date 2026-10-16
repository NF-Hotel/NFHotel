using System.Reflection;
using System.Text.RegularExpressions;

using NFHotel.Domain.Entities;

namespace NFHotel.Application.Tests.Architecture;

/// <summary>
/// Enforces the folder and type structure from ADR-0001: entities on <see cref="Entity{TKey}"/>,
/// the folders of the skill per layer, repository interfaces in Domain and DTOs in one folder per entity.
/// </summary>
public sealed partial class StructureTests
{
    private static readonly Assembly DomainAssembly = typeof(Entity<>).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(UseCases.Result).Assembly;

    private static readonly string[] DomainFolders =
    [
        "NFHotel.Domain.Entities",
        "NFHotel.Domain.Errors",
        "NFHotel.Domain.Repositories",
        "NFHotel.Domain.ValueObjects",
    ];

    private static readonly string[] DtoFolders =
    [
        "NFHotel.Application.Dto.Bookings",
        "NFHotel.Application.Dto.Guests",
        "NFHotel.Application.Dto.Holidays",
        "NFHotel.Application.Dto.Rooms",
    ];

    #region Domain structure
    [Fact]
    public void Entities_EveryConcreteType_DerivesFromEntityOfTKey()
    {
        var entities = DomainAssembly.GetTypes()
            .Where(t => t.Namespace == "NFHotel.Domain.Entities" && t is { IsClass: true, IsAbstract: false })
            .ToList();

        Assert.NotEmpty(entities);
        var offenders = entities.Where(t => !DerivesFromEntity(t)).Select(t => t.Name).ToArray();
        Assert.True(offenders.Length == 0, $"Disse entiteter arver ikke Entity<TKey>: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Domain_EveryType_IsInOneOfTheSkillFolders()
    {
        var offenders = DomainAssembly.GetTypes()
            .Where(t => !IsCompilerGenerated(t) && !DomainFolders.Contains(t.Namespace))
            .Select(t => $"{t.Namespace}.{t.Name}")
            .ToArray();

        Assert.True(offenders.Length == 0, $"Typer uden for skillets mapper: {string.Join(", ", offenders)}");
    }


    [Fact]
    public void Domain_HasNoRulesNamespaceOrRulesClass()
    {
        var offenders = DomainAssembly.GetTypes()
            .Where(t => !IsCompilerGenerated(t)
                && (t.Namespace?.EndsWith(".Rules", StringComparison.Ordinal) == true
                    || t.Name.EndsWith("Rules", StringComparison.Ordinal)))
            .Select(t => $"{t.Namespace}.{t.Name}")
            .ToArray();

        Assert.True(offenders.Length == 0, $"Reglerne hører til entiteter, værdiobjekter og fejl: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Entities_HaveNoValidateOrIsValidMethods()
    {
        var offenders = DomainAssembly.GetTypes()
            .Where(t => t.Namespace == "NFHotel.Domain.Entities")
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(m => (Type: t.Name, Method: m.Name)))
            .Where(x => x.Method == "Validate" || x.Method.StartsWith("IsValid", StringComparison.Ordinal))
            .Select(x => $"{x.Type}.{x.Method}")
            .ToArray();

        Assert.True(offenders.Length == 0, $"The rules are attributes (ADR-0002), not methods: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Domain_ReferencesOnlyTheFrameworkClassLibrary()
    {
        var offenders = DomainAssembly.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(n => !n.StartsWith("System", StringComparison.Ordinal) && n is not ("netstandard" or "mscorlib"))
            .ToArray();

        Assert.True(offenders.Length == 0, $"Domain references more than the framework: {string.Join(", ", offenders)}");
    }

    #endregion

    #region Application structure
    [Fact]
    public void Repositories_EveryInterface_IsDeclaredInDomain()
    {
        var misplaced = ApplicationAssembly.GetTypes()
            .Where(t => t.IsInterface && t.Name.EndsWith("Repository", StringComparison.Ordinal))
            .Select(t => t.Name)
            .ToArray();
        var domainRepositories = DomainAssembly.GetTypes()
            .Where(t => t.IsInterface && t.Name.EndsWith("Repository", StringComparison.Ordinal))
            .ToList();

        Assert.Empty(misplaced);
        Assert.NotEmpty(domainRepositories);
        Assert.All(domainRepositories, t => Assert.Equal("NFHotel.Domain.Repositories", t.Namespace));
    }

    [Fact]
    public void Dto_EveryType_IsInOneFolderPerEntity()
    {
        var dtoTypes = ApplicationAssembly.GetTypes()
            .Where(t => !IsCompilerGenerated(t) && t.Namespace is not null
                        && t.Namespace.StartsWith("NFHotel.Application.Dto", StringComparison.Ordinal))
            .ToList();
        var strays = ApplicationAssembly.GetTypes()
            .Where(t => !IsCompilerGenerated(t) && t.Name.EndsWith("Dto", StringComparison.Ordinal)
                        && !DtoFolders.Contains(t.Namespace))
            .Select(t => t.Name)
            .ToArray();

        Assert.NotEmpty(dtoTypes);
        Assert.All(dtoTypes, t => Assert.Contains(t.Namespace, DtoFolders));
        Assert.Empty(strays);
    }

    [Fact]
    public void Application_EveryType_IsInOneOfTheSkillFolders()
    {
        var offenders = ApplicationAssembly.GetTypes()
            .Where(t => !IsCompilerGenerated(t) && !IsInApplicationFolder(t.Namespace))
            .Select(t => $"{t.Namespace}.{t.Name}")
            .ToArray();

        Assert.True(offenders.Length == 0, $"Typer uden for skillets mapper: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Dto_EverySourceFile_DeclaresOneTopLevelType()
    {
        var dtoDirectory = Path.Combine(FindRepositoryRoot(), "src", "NFHotel.Application", "Dto");
        var files = Directory.GetFiles(dtoDirectory, "*.cs", SearchOption.AllDirectories);

        Assert.NotEmpty(files);
        var offenders = files
            .Where(file => TopLevelType().Matches(File.ReadAllText(file)).Count != 1)
            .Select(file => Path.GetRelativePath(dtoDirectory, file))
            .ToArray();
        Assert.True(offenders.Length == 0, $"Filer med ikke præcis én type: {string.Join(", ", offenders)}");
    }

    #endregion

    private static bool DerivesFromEntity(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Entity<>))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCompilerGenerated(Type type) =>
        type.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() is not null
        || type.Name.StartsWith('<')
        || (type.DeclaringType is not null && IsCompilerGenerated(type.DeclaringType));

    private static bool IsInApplicationFolder(string? ns) =>
        ns is "NFHotel.Application.Errors" or "NFHotel.Application.Ports" or "NFHotel.Application.UseCases"
        || (ns is not null && (ns.StartsWith("NFHotel.Application.Dto.", StringComparison.Ordinal)
                               || ns.StartsWith("NFHotel.Application.UseCases.", StringComparison.Ordinal)));

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "NFHotel.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("NFHotel.sln was not found above the test output folder.");
    }

    [GeneratedRegex(@"^public\s+(?:sealed\s+|static\s+|abstract\s+)*(?:record|class|enum|interface|struct)\b", RegexOptions.Multiline)]
    private static partial Regex TopLevelType();
}