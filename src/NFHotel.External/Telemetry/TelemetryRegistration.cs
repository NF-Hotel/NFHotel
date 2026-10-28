using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace NFHotel.External.Telemetry;

/// <summary>
/// Sends the metrics of a host (the web application or the API) to the monitoring over OTLP (MIL-034, ADR-0023): the
/// request rate, duration and status that ASP.NET Core measures itself, the unhandled exceptions, and the runtime. The
/// metrics are pushed to Alloy on the internal network of the applications, so no port of an application is opened for
/// scraping. Without <c>Telemetry:OtlpEndpoint</c> nothing is sent, as in the tests and in local development.
/// </summary>
public static class TelemetryRegistration
{
    /// <summary>The configuration key with the OTLP/HTTP address of Alloy, for example <c>http://alloy:4318</c>.</summary>
    public const string OtlpEndpointSetting = "Telemetry:OtlpEndpoint";

    /// <summary>How often the metrics are sent: as often as Prometheus scrapes the host metrics.</summary>
    public const int ExportIntervalMilliseconds = 30_000;

    /// <summary>
    /// The meters that are sent. ASP.NET Core publishes the request duration on <c>Microsoft.AspNetCore.Hosting</c>,
    /// the unhandled exceptions on <c>Microsoft.AspNetCore.Diagnostics</c> and the connections on
    /// <c>Microsoft.AspNetCore.Server.Kestrel</c>; .NET publishes memory, GC and threads on <c>System.Runtime</c>.
    /// </summary>
    public static IReadOnlyList<string> Meters { get; } =
    [
        "Microsoft.AspNetCore.Hosting",
        "Microsoft.AspNetCore.Diagnostics",
        "Microsoft.AspNetCore.Server.Kestrel",
        "System.Runtime",
    ];

    /// <summary>Registers the metrics and their OTLP export when an endpoint is configured.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration, with <see cref="OtlpEndpointSetting"/>.</param>
    /// <param name="serviceName">The name of the host in the metrics (the label <c>job</c> in Prometheus).</param>
    /// <returns>The service collection.</returns>
    /// <exception cref="InvalidOperationException">The endpoint is set but is not an absolute HTTP address.</exception>
    public static IServiceCollection AddServiceTelemetry(this IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        var endpoint = configuration[OtlpEndpointSetting];
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return services;
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var baseAddress) || baseAddress.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException($"{OtlpEndpointSetting} must be an absolute http or https address, not '{endpoint}'.");
        }

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithMetrics(metrics => metrics
                .AddMeter([.. Meters])
                .AddOtlpExporter((exporter, reader) =>
                {
                    exporter.Endpoint = new Uri(baseAddress, "v1/metrics");
                    exporter.Protocol = OtlpExportProtocol.HttpProtobuf;
                    reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = ExportIntervalMilliseconds;
                }));
        return services;
    }
}
