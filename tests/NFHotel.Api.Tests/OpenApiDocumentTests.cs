using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NFHotel.Api.Contracts;
using NFHotel.Api.Startup;
using NFHotel.TestSupport;

namespace NFHotel.Api.Tests;

/// <summary>The OpenAPI document of the API: written when the API is built, and never served (MIL-077).</summary>
public sealed class OpenApiDocumentTests : IClassFixture<ApiFactory>
{
    private static readonly string[] OperationNames = ["get", "put", "post", "delete", "patch", "head", "options", "trace"];
    private static readonly string TokenKey = $"{ApiAuthentication.SectionName}:{ApiAuthentication.TokenName}";
    private readonly ApiFactory _factory;

    /// <summary>Initializes a new instance of the <see cref="OpenApiDocumentTests"/> class.</summary>
    /// <param name="factory">The test API.</param>
    public OpenApiDocumentTests(ApiFactory factory) => _factory = factory;

    #region The document

    /// <summary>Positive: the document that the build writes names every endpoint that the API maps, and nothing else.</summary>
    [Fact]
    public void BuildOutput_OpenApiDocument_NamesEveryEndpoint()
    {
        // Arrange
        using var document = ReadDocument();
        var endpoints = _factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .SelectMany(endpoint => (endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [])
                .Select(method => $"{method} {DocumentPath(endpoint.RoutePattern)}"))
            .Order(StringComparer.Ordinal)
            .ToArray();

        // Act
        var operations = document.RootElement.GetProperty("paths").EnumerateObject()
            .SelectMany(pathItem => pathItem.Value.EnumerateObject()
                .Where(member => OperationNames.Contains(member.Name, StringComparer.Ordinal))
                .Select(member => $"{member.Name.ToUpperInvariant()} {pathItem.Name}"))
            .Order(StringComparer.Ordinal)
            .ToArray();

        // Assert
        Assert.NotEmpty(endpoints);
        Assert.Equal(endpoints, operations);
    }

    /// <summary>Positive: the document is OpenAPI 3.0, the version that the API scan of OWASP ZAP reads.</summary>
    [Fact]
    public void BuildOutput_OpenApiVersion_IsTheOneZapReads()
    {
        // Arrange
        using var document = ReadDocument();

        // Act
        var version = document.RootElement.GetProperty("openapi").GetString();

        // Assert
        Assert.StartsWith("3.0.", version, StringComparison.Ordinal);
    }

    /// <summary>Negative: the API in Production does not serve the document; a client with the API token gets 404.</summary>
    [Fact]
    public async Task OpenApiDocument_InProduction_IsNotServed()
    {
        // Arrange
        using var production = new ProductionFactory();
        Assert.Equal(Environments.Production, production.Services.GetRequiredService<IWebHostEnvironment>().EnvironmentName);
        using var client = production.CreateServiceClient();

        // Act
        using var response = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Stand-in settings

    /// <summary>Positive: the stand-in settings alone start the API, as the build of the document needs; it answers its health check.</summary>
    [Fact]
    public async Task StandInSettings_Alone_StartTheApi()
    {
        // Arrange
        var settings = OpenApiDocumentBuild.StandInSettings();
        await using var factory = new StandInFactory(settings);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", settings[TokenKey]);

        // Act
        using var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>Edge case: each build gets new random keys, so the source holds no key and no build repeats one.</summary>
    [Fact]
    public void StandInSettings_EachBuild_GetsNewKeys()
    {
        // Act
        var first = OpenApiDocumentBuild.StandInSettings();
        var second = OpenApiDocumentBuild.StandInSettings();

        // Assert
        Assert.Equal(first.Keys, second.Keys);
        Assert.NotEqual(first[TokenKey], second[TokenKey]);
    }

    /// <summary>Negative: outside the build of the document the API runs with its own settings, not the stand-ins.</summary>
    [Fact]
    public void IsRunning_OutsideTheDocumentBuild_IsFalse()
    {
        // Act
        var isRunning = OpenApiDocumentBuild.IsRunning;

        // Assert
        Assert.False(isRunning);
    }

    #endregion

    #region Helpers

    private static JsonDocument ReadDocument()
    {
        var path = Path.Join(RepositoryRoot.Find(), "g", "openapi", "nfhotel-api.json");
        Assert.True(File.Exists(path), $"The build of the API wrote no OpenAPI document to {path}.");
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    // The path of an endpoint as the document writes it: a parameter by its name, without its constraints.
    private static string DocumentPath(RoutePattern pattern) =>
        "/" + string.Join('/', pattern.PathSegments.Select(segment => string.Concat(segment.Parts.Select(part => part switch
        {
            RoutePatternParameterPart parameter => $"{{{parameter.Name}}}",
            RoutePatternLiteralPart literal => literal.Content,
            RoutePatternSeparatorPart separator => separator.Content,
            _ => string.Empty,
        }))));

    // The test API in Production, with the stand-ins of the in-memory world of the tests.
    private sealed class ProductionFactory : ApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseEnvironment(Environments.Production);
        }
    }

    // The API itself in Production, with no settings but the stand-ins of the build of the document.
    private sealed class StandInFactory(Dictionary<string, string?> settings) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(Environments.Production);
            foreach (var (key, value) in settings)
            {
                builder.UseSetting(key, value);
            }
        }
    }

    #endregion
}
