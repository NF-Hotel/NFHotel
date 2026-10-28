using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NFHotel.External.Telemetry;

using OpenTelemetry.Metrics;

namespace NFHotel.External.Tests.Telemetry;

/// <summary>The metrics of a host are sent to the monitoring only when an OTLP endpoint is configured (MIL-034).</summary>
public sealed class TelemetryRegistrationTests
{
    /// <summary>Positive: with an endpoint, a meter provider is built for the host.</summary>
    [Fact]
    public void AddServiceTelemetry_WithEndpoint_BuildsAMeterProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddServiceTelemetry(Configuration("http://alloy:4318"), "nfhotel-test");
        using var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<MeterProvider>());
    }

    /// <summary>Equivalence: without an endpoint, as in the tests and in local development, nothing is registered.</summary>
    /// <param name="endpoint">The configured value.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddServiceTelemetry_WithoutEndpoint_RegistersNothing(string? endpoint)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddServiceTelemetry(Configuration(endpoint), "nfhotel-test");

        // Assert
        Assert.Empty(services);
    }

    /// <summary>Negative: an endpoint that is not an absolute http or https address stops the start.</summary>
    /// <param name="endpoint">The configured value.</param>
    [Theory]
    [InlineData("alloy:4318")]
    [InlineData("ftp://alloy:4318")]
    [InlineData("/v1/metrics")]
    public void AddServiceTelemetry_WithInvalidEndpoint_Throws(string endpoint)
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => services.AddServiceTelemetry(Configuration(endpoint), "nfhotel-test"));

        // Assert
        Assert.Contains(TelemetryRegistration.OtlpEndpointSetting, error.Message, StringComparison.Ordinal);
    }

    /// <summary>Positive: the meters of ASP.NET Core requests and unhandled exceptions are among those sent.</summary>
    [Fact]
    public void Meters_IncludeRequestsAndExceptions_ForTheObjectives()
    {
        // Act
        var meters = TelemetryRegistration.Meters;

        // Assert
        Assert.Contains("Microsoft.AspNetCore.Hosting", meters);
        Assert.Contains("Microsoft.AspNetCore.Diagnostics", meters);
    }

    private static IConfiguration Configuration(string? endpoint) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [TelemetryRegistration.OtlpEndpointSetting] = endpoint })
            .Build();
}
