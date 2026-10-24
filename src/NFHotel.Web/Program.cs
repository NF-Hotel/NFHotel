using NFHotel.ApiClient;
using NFHotel.External;
using NFHotel.Web.Common;
using NFHotel.Web.Components;
using NFHotel.Web.Features.Admin;
using NFHotel.Web.Features.Booking;
using NFHotel.Web.Security;

var builder = WebApplication.CreateBuilder(args);

// Reads Docker Secrets as configuration from files in the container.
builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);

// ----------------------------------------------------------------------
// COMPOSITION ROOT
//
// Conventions.md: Program.cs is the composition root. No other file in Web registers
// dependencies, and no component news up a service.
// ----------------------------------------------------------------------

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Infrastructure registers the DbContext factory, repositories, IUnitOfWork, IClock,
// IStringEncryptor and the three Application services. Web knows only this one call.
// It also validates the encryption key and the time zone at start-up, so a missing key
// stops the app here rather than at the first guest update (B-09, A-05).
// The use cases come from the database API, never from the database (ADR-0004).
builder.Services.AddApiClient(builder.Configuration);
builder.Services.AddHotelClock(builder.Configuration);
builder.Services.AddHolidayApi(builder.Configuration);
builder.Services.AddBenchmarkService(builder.Configuration);   // starts runs on the benchmark service (MIL-020)

// Open generic scoped service: gives every use case call its own DI scope (Architecture.md section 8). The runner is
// stateless, but it belongs to the circuit so that it can hand the staff token of the signed-in user to each call
// (ADR-0005).
builder.Services.AddScoped(typeof(IUseCaseRunner<>), typeof(UseCaseRunner<>));

// The staff sign-in: the cookie, one policy per permission, and the source of the staff token (ADR-0005).
var requireTls = HttpsRequirement.IsRequired(builder.Configuration);
builder.Services.AddStaffSignIn(requireTls);
builder.Services.AddSecureAntiforgery(requireTls);   // the antiforgery cookie follows the staff cookie (MIL-033)
builder.Services.AddHttpContextAccessor();

// ViewModels are Scoped: in Blazor Server a scope is one SignalR circuit, that is, one user's
// session in one tab. That is exactly the lifetime the state of the screen should have.
//
// Each UI area registers its own — so adding a screen touches one file,
// not the composition root.
builder.Services.AddBookingFeature();   // the customer's booking flow: /book and /booking/{number}
builder.Services.AddAdminFeature();     // the staff's back office: /admin/**, behind the sign-in

var app = builder.Build();

// First in the pipeline, so every response gets the browser security headers, redirects and error pages included
// (MIL-033).
app.UseSecurityHeaders();

if (!app.Environment.IsDevelopment())
{
    // createScopeForErrors: the error page gets its own scope and does not inherit a DbContext that
    // just failed.
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Behind the TLS proxy (Tls:Require): a request that is still HTTP is sent to HTTPS (ADR-0006).
app.UseHttpsRequirement(requireTls);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMustChangePassword();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapStaffSignOut();
// Blazor's own policy (frame-ancestors 'self') is turned off: UseSecurityHeaders sends the complete one.
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode(options => options.ContentSecurityFrameAncestorsPolicy = null);

app.Run();
/// <summary>The entry point, public so that the integration tests can start the web application.</summary>
public partial class Program;
