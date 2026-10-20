using System.Reflection;

using NFHotel.Application.UseCases;

namespace NFHotel.Application.Tests.Architecture;

/// <summary>
/// Enforces the dependency direction of Clean Architecture. These rules cannot
/// be enforced by discipline alone — one innocent using in a busy week is enough to
/// reverse the direction, and then neither a later API nor the mobile app inherits the rules for free.
/// Cf. Architecture.md sections 2 and 8.
/// </summary>
public sealed class LayeringTests
{
    private static readonly Assembly ApplicationAssembly = typeof(Result).Assembly;
    private static readonly Assembly DomainAssembly = typeof(Domain.Entities.Booking).Assembly;

    /// <summary>Application may know only Domain and the BCL — no persistence, no web.</summary>
    [Theory]
    [InlineData("Microsoft.EntityFrameworkCore")]
    [InlineData("Npgsql")]
    [InlineData("Microsoft.AspNetCore")]
    [InlineData("Microsoft.Extensions.DependencyInjection")]
    public void Application_DoesNotReference_InfrastructureOrWebPackages(string forbiddenPrefix)
    {
        var offenders = ApplicationAssembly
            .GetReferencedAssemblies()
            .Where(a => a.Name is not null &&
                        a.Name.StartsWith(forbiddenPrefix, StringComparison.Ordinal))
            .Select(a => a.Name)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            $"Application references {string.Join(", ", offenders)}. " +
            "The business logic must be callable without a persistence or web layer.");
    }

    /// <summary>Domain is the core: zero dependencies beyond the BCL.</summary>
    [Fact]
    public void Domain_WhenInspected_HasNoOutgoingDependencies()
    {
        var offenders = DomainAssembly
            .GetReferencedAssemblies()
            .Where(a => a.Name is not null &&
                        !a.Name.StartsWith("System", StringComparison.Ordinal) &&
                        !a.Name.Equals("netstandard", StringComparison.Ordinal) &&
                        !a.Name.Equals("mscorlib", StringComparison.Ordinal))
            .Select(a => a.Name)
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            $"Domain refererer {string.Join(", ", offenders)}. Kernen skal have udgrad nul.");
    }

    /// <summary>
    /// Application must not expose Domain entities directly out of its services —
    /// the UI and a later API must speak DTOs, cf. Conventions.md.
    /// </summary>
    [Fact]
    public void Services_WhenInspected_DoNotReturnDomainEntities()
    {
        var domainNamespace = DomainAssembly.GetName().Name!;

        var leaks = ApplicationAssembly.GetTypes()
            .Where(t => t.IsInterface && t.Name.EndsWith("Service", StringComparison.Ordinal))
            .SelectMany(t => t.GetMethods())
            .Select(m => new { m.DeclaringType, m.Name, Returned = Unwrap(m.ReturnType) })
            .Where(x => x.Returned.Assembly.GetName().Name == domainNamespace
                        && !x.Returned.IsEnum)
            .Select(x => $"{x.DeclaringType!.Name}.{x.Name} → {x.Returned.Name}")
            .ToArray();

        Assert.True(
            leaks.Length == 0,
            $"These service methods leak domain entities: {string.Join(", ", leaks)}");
    }

    /// <summary>Unwraps Task&lt;T&gt;, Result&lt;T&gt; and IReadOnlyList&lt;T&gt; to the underlying type.</summary>
    private static Type Unwrap(Type type)
    {
        while (type.IsGenericType)
        {
            type = type.GetGenericArguments()[0];
        }

        return type;
    }
}
