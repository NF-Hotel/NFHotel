using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using SecurityPrototype.Data;
using SecurityPrototype.Security;
using SecurityPrototype.Models;
using Microsoft.AspNetCore.Authentication.Cookies;


var builder = WebApplication.CreateBuilder(args);

//bruger builder til at hente en connection string fra PostgreSQL-database.
var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' not found.");

// Registrerer ApplicationDbContext i Dependency Injection.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));



// Registrerer ASP.NET Core Identity med AppUser som brugerklasse
builder.Services.AddIdentityCore<AppUser>(options =>
{
    // Konfiguration af password-regler
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 12;
})
    .AddRoles<IdentityRole>() // Tilføjer rolle-støtte
    .AddEntityFrameworkStores<ApplicationDbContext>() // Bruger ApplicationDbContext til at gemme brugere og roller
    .AddSignInManager(); // Tilføjer SignInManager til håndtering af login




// Registrerer og konfigurerer authorization med rollebaserede policies.
builder.Services.AddAuthorization(options =>
{
    // opsat på den måde, så kun Manager og Receptionist må få adgang til pas data.
    options.AddPolicy("CanAccessPassportData", policy =>
        policy.RequireRole(Roles.Manager, Roles.Receptionist));


    // opsat på den måde, så kun Manager, Receptionist og Housekeeping må få adgang til housekeeping data.
    options.AddPolicy("CanAccessHousekeeping", policy =>
        policy.RequireRole(
            Roles.Manager,
            Roles.Receptionist,
            Roles.Housekeeping));
});


// Aktiverer authentication(nødvendig for cookies/login)
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();



// Konfigurerer indstillingerne for ASP.NET Core Identity authentication-cookie.
// IdentityConstants.ApplicationScheme angiver specifikt den cookie, som Identity bruger til at holde en bruger logget ind.
builder.Services.Configure<CookieAuthenticationOptions>(
    IdentityConstants.ApplicationScheme,
    options =>
    {

        // Denne event bliver kørt når en bruger forsøger at tilgå
        // et beskyttet endpoint uden at være logget ind.
        options.Events.OnRedirectToLogin = context =>
        {

            // Returnerer HTTP-statuskoden 401 Unauthorized i stedet for at redirecte brugeren til en login-side.
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;

            // Fortæller at den asynkrone event-handler er færdig.
            return Task.CompletedTask;
        };

        // Denne event bliver kørt, når brugeren er logget ind,
        options.Events.OnRedirectToAccessDenied = context =>
        {
            // Returnerer HTTP-statuskoden 403 Forbidden
            context.Response.StatusCode = StatusCodes.Status403Forbidden;

            // Fortæller at den asynkrone event-handler er færdig.
            return Task.CompletedTask;
        };
    });







/**************************************************************/
//runtime secktion

var app = builder.Build();

// ===== Seed database with roles =====

// Opretter et midlertidigt Dependency Injection-scope.
// Et scope bruges, så vi kan hente scoped services (Identity/EF Core-services)
// fra appens DI-container
using (var scope = app.Services.CreateScope())
{
    // Kalder DbSeeder og sender scopets ServiceProvider med.
    // SeedAsync kontrollerer derefter, om rollerne findes, og opretter dem hvis de mangler.
    await DbSeeder.SeedAsync(scope.ServiceProvider, builder.Configuration);

}


app.UseAuthentication();
app.UseAuthorization();


// ===== Endpoints =====
// Login (cookie udstedes ved succes)
app.MapPost("/login", async (LoginRequest req,
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager) =>
{
    var user = await userManager.FindByEmailAsync(req.Email);
    if (user is null) return Results.Unauthorized();

    var result = await signInManager.CheckPasswordSignInAsync(user, req.Password, false);
    if (!result.Succeeded) return Results.Unauthorized();

    await signInManager.SignInAsync(user, isPersistent: false);
    return Results.Ok($"Logget ind som {req.Email}");
});


// Logout
// Opretter et POST-endpoint til logout.
app.MapPost("/logout", async (SignInManager<AppUser> signInManager) =>
{
    // Signer brugeren ud og fjerner cookie auth
    await signInManager.SignOutAsync();
    return Results.Ok("Logget ud");
});



// beskyttet GET-endpoin hvor kun brugere der opfylder "CanAccessPassportData"-policy får adgang.
// Passport data endpoint kun tilgængelig for Manager og Receptionist
// Opretter et GET-endpoint på URL'en /admin/passportdata.
//Map = forbind / map en URL til noget kode.
app.MapGet("/admin/passportdata", () =>
{
    return Results.Ok("Fake passport data");
})
    .RequireAuthorization("CanAccessPassportData");

// Beskyttet endpoint til housekeeping-data.
// Tilgængelig for Manager Receptionist og Housekeeping.
app.MapGet("/admin/housekeeping", () =>
{
    return Results.Ok("Fake housekeeping data");
})
.RequireAuthorization("CanAccessHousekeeping");


app.Run();
