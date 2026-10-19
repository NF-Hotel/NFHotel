using Microsoft.AspNetCore.Diagnostics;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Api.Startup;
using NFHotel.External;
using NFHotel.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);

builder.Services.AddControllers();
builder.Services.AddSingleton(ApiToken.Read(builder.Configuration));
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHotelClock(builder.Configuration);
builder.Services.AddMarketAnalysisService(builder.Configuration);   // starts and reads runs of the market analysis (MIL-026)
builder.Services.AddStaffAuthentication();

var app = builder.Build();

// A key below one or a missing argument is a programming error of the caller: 400. Anything else is a
// fault of the API: 500, without a stack trace or a message that could leak a setting.
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var invalid = error is ArgumentException;
    context.Response.StatusCode = invalid ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(
        new ApiResponse([invalid ? "api.invalid_request" : "api.unexpected_error"]),
        ApiJson.Options,
        context.RequestAborted).ConfigureAwait(false);
}));

app.UseMiddleware<ApiTokenMiddleware>();
app.UseAuthentication();

await ApiStartup.MigrateIfRequestedAsync(app, app.Lifetime.ApplicationStopping).ConfigureAwait(false);
await ApiStartup.EnsureFirstManagerAsync(app, app.Lifetime.ApplicationStopping).ConfigureAwait(false);

if (ApiStartup.IsCreateStaff(args))
{
    return await ApiStartup.RunCreateStaffAsync(
        app.Services, args, Environment.GetEnvironmentVariable(ApiStartup.PasswordVariable), app.Lifetime.ApplicationStopping).ConfigureAwait(false);
}

app.MapGet($"/{ApiRoutes.Health}", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

await app.RunAsync().ConfigureAwait(false);
return 0;

/// <summary>The entry point, public so that the integration tests can start the API.</summary>
public partial class Program;
