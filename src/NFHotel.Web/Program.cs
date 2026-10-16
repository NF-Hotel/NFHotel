using NFHotel.External;
using NFHotel.Infrastructure;
using NFHotel.Web.Common;
using NFHotel.Web.Components;
using NFHotel.Web.Features.Admin;
using NFHotel.Web.Features.Booking;

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
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHotelClock(builder.Configuration);
builder.Services.AddHolidayApi(builder.Configuration);

// Open generic singleton: gives every use case call its own DI scope and thereby its own
// DbContext (Architecture.md section 8). The runner is stateless — it is the scope it makes
// that is short-lived, not the runner itself.
builder.Services.AddSingleton(typeof(IUseCaseRunner<>), typeof(UseCaseRunner<>));

// ViewModels are Scoped: in Blazor Server a scope is one SignalR circuit, that is, one user's
// session in one tab. That is exactly the lifetime the state of the screen should have.
//
// Each UI area registers its own — so adding a screen touches one file,
// not the composition root.
builder.Services.AddBookingFeature();   // the customer's booking flow: /book and /booking/{number}
builder.Services.AddAdminFeature();     // the staff's back office: /admin/**

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    // createScopeForErrors: the error page gets its own scope and does not inherit a DbContext that
    // just failed.
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();