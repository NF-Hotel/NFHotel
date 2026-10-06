using NFHotel.Infrastructure;
using NFHotel.Web.Common;
using NFHotel.Web.Components;
using NFHotel.Web.Features.Admin;
using NFHotel.Web.Features.Booking;

var builder = WebApplication.CreateBuilder(args);

// Læser Docker Secrets som konfiguration fra filer i containeren.
builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);

// ----------------------------------------------------------------------
// COMPOSITION ROOT
//
// Conventions.md: Program.cs er composition root. Ingen anden fil i Web registrerer
// afhængigheder, og ingen komponent new'er en service.
// ----------------------------------------------------------------------

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Infrastructure registrerer DbContext-fabrikken, repositories, IUnitOfWork, IClock,
// IStringEncryptor og de tre Application-services. Web kender kun dette ene kald.
// Den validerer også krypteringsnøgle og tidszone ved opstart, så en manglende nøgle
// stopper appen her frem for ved den første gæsteopdatering (B-09, A-05).
builder.Services.AddInfrastructure(builder.Configuration);

// Åben generisk singleton: giver hvert use case-kald sin egen DI-scope og dermed sin egen
// DbContext (Architecture.md afsnit 8). Runneren er tilstandsløs — det er scopen den laver
// der er kortlivet, ikke runneren selv.
builder.Services.AddSingleton(typeof(IUseCaseRunner<>), typeof(UseCaseRunner<>));

// ViewModels er Scoped: i Blazor Server er en scope ét SignalR-kredsløb, altså én brugers
// session i én fane. Det er præcis den levetid skærmens tilstand skal have.
//
// Hvert UI-område registrerer sine egne — så tilføjer man en skærm, rører man én fil,
// ikke composition rooten.
builder.Services.AddBookingFeature();   // kundens bookingflow: /book og /booking/{nummer}
builder.Services.AddAdminFeature();     // personalets backoffice: /admin/**

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    // createScopeForErrors: fejlsiden får sin egen scope og arver ikke en DbContext der
    // netop fejlede.
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
