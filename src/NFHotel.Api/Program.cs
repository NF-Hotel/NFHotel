using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NFHotel.Api;
using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Api.Startup;
using NFHotel.Application.Ports;
using NFHotel.External;
using NFHotel.External.Telemetry;
using NFHotel.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);

// The build of the OpenAPI document starts the API without its settings; they get stand-ins that reach nothing (MIL-077).
if (OpenApiDocumentBuild.IsRunning)
{
    builder.Configuration.AddInMemoryCollection(OpenApiDocumentBuild.StandInSettings());
}

// Every value bound from a request meets the input rules before an action runs, and a request that cannot be bound
// is refused with the code of the API alone, without the names of .NET types (MIL-077).
builder.Services.AddControllers(options => options.Filters.Add<InputGuardFilter>())
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory =
        _ => new BadRequestObjectResult(new ApiResponse([ApiErrorCodes.InvalidRequest])));

// The OpenAPI document is written when the API is built (g/openapi/nfhotel-api.json, see the project file). The
// running API does not serve it: there is no MapOpenApi (MIL-077).
builder.Services.AddOpenApi();
builder.Services.AddSingleton(ApiToken.Read(builder.Configuration));
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHotelClock(builder.Configuration);
builder.Services.AddServiceTelemetry(builder.Configuration, "nfhotel-api");   // metrics to the monitoring (MIL-034)
builder.Services.AddMarketAnalysisService(builder.Configuration);   // the markets API that analyses the bookings (MIL-026)
builder.Services.AddSingleton<ChannelMarketAnalysisRunLauncher>();
builder.Services.AddSingleton<IMarketAnalysisRunLauncher>(provider => provider.GetRequiredService<ChannelMarketAnalysisRunLauncher>());
builder.Services.AddHostedService<MarketAnalysisRunWorker>();
builder.Services.AddStaffAuthentication();
builder.Services.AddHttpContextAccessor();
builder.Services.Replace(ServiceDescriptor.Scoped<IStaffCallContext, HttpStaffCallContext>());
builder.Services.AddStaffWriteRateLimit();
builder.Services.AddSingleton<AddressLoginThrottle>();   // the limit per client address for the mobile app (ADR-0015)
builder.Services.AddScoped<LoginAddressLimitFilter>();

// Behind the proxy the peer is nginx; the address of the phone is in X-Forwarded-For. The API publishes no port, so
// only the proxy and the web application on the internal networks can send the header (ADR-0015).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Every answer, a refusal and a fault included, tells a browser not to guess its type (MIL-077).
app.UseApiSecurityHeaders();
app.UseForwardedHeaders();

// A key below one or a missing argument is a programming error of the caller: 400. Anything else is a
// fault of the API: 500, without a stack trace or a message that could leak a setting.
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var invalid = error is ArgumentException;
    context.Response.StatusCode = invalid ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(
        new ApiResponse([invalid ? ApiErrorCodes.InvalidRequest : ApiErrorCodes.UnexpectedError]),
        ApiJson.Options,
        context.RequestAborted).ConfigureAwait(false);
}));

app.UseMiddleware<ApiTokenMiddleware>();
app.UseAuthentication();
app.UseRateLimiter();

await ApiStartup.MigrateIfRequestedAsync(app, app.Lifetime.ApplicationStopping).ConfigureAwait(false);
await ApiStartup.EnsureFirstManagerAsync(app, app.Lifetime.ApplicationStopping).ConfigureAwait(false);

if (ApiStartup.IsCreateStaff(args))
{
    return await ApiStartup.RunCreateStaffAsync(
        app.Services, args, Environment.GetEnvironmentVariable(ApiStartup.PasswordVariable), app.Lifetime.ApplicationStopping).ConfigureAwait(false);
}

if (ApiStartup.IsResetStaffPassword(args))
{
    return await ApiStartup.RunResetStaffPasswordAsync(
        app.Services, args, Environment.GetEnvironmentVariable(ApiStartup.PasswordVariable), app.Lifetime.ApplicationStopping).ConfigureAwait(false);
}

if (ApiStartup.IsSeedMarketData(args))
{
    return await ApiStartup.RunSeedMarketDataAsync(app.Services, args, app.Lifetime.ApplicationStopping).ConfigureAwait(false);
}

app.MapGet($"/{ApiRoutes.Health}", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

await app.RunAsync().ConfigureAwait(false);
return 0;

/// <summary>The entry point, public so that the integration tests can start the API.</summary>
public partial class Program;
