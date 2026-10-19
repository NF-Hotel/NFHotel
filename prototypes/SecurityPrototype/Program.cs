using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using SecurityPrototype.Data;
using SecurityPrototype.Security;
using SecurityPrototype.Models;
using Microsoft.AspNetCore.Authentication.Cookies;


var builder = WebApplication.CreateBuilder(args);

//uses builder to fetch a connection string for the PostgreSQL database.
var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' not found.");

// Registers ApplicationDbContext in Dependency Injection.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));



// Registers ASP.NET Core Identity with AppUser as the user class
builder.Services.AddIdentityCore<AppUser>(options =>
{
    // Configuration of the password rules
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 12;
})
    .AddRoles<IdentityRole>() // Adds role support
    .AddEntityFrameworkStores<ApplicationDbContext>() // Uses ApplicationDbContext to store users and roles
    .AddSignInManager(); // Adds SignInManager to handle sign-in




// Registers and configures authorization with role-based policies.
builder.Services.AddAuthorization(options =>
{
    // set up so that only Manager and Receptionist may get access to passport data.
    options.AddPolicy("CanAccessPassportData", policy =>
        policy.RequireRole(Roles.Manager, Roles.Receptionist));


    // set up so that only Manager, Receptionist and Housekeeping may get access to housekeeping data.
    options.AddPolicy("CanAccessHousekeeping", policy =>
        policy.RequireRole(
            Roles.Manager,
            Roles.Receptionist,
            Roles.Housekeeping));
});


// Enables authentication (needed for cookies/sign-in)
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();



// Configures the settings of the ASP.NET Core Identity authentication cookie.
// IdentityConstants.ApplicationScheme names the cookie that Identity uses to keep a user signed in.
builder.Services.Configure<CookieAuthenticationOptions>(
    IdentityConstants.ApplicationScheme,
    options =>
    {

        // This event runs when a user tries to reach
        // a protected endpoint without being signed in.
        options.Events.OnRedirectToLogin = context =>
        {

            // Returns HTTP status code 401 Unauthorized instead of redirecting the user to a login page.
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;

            // Tells that the asynchronous event handler has finished.
            return Task.CompletedTask;
        };

        // This event runs when the user is signed in,
        options.Events.OnRedirectToAccessDenied = context =>
        {
            // Returns HTTP status code 403 Forbidden
            context.Response.StatusCode = StatusCodes.Status403Forbidden;

            // Tells that the asynchronous event handler has finished.
            return Task.CompletedTask;
        };
    });







/**************************************************************/
//runtime secktion

var app = builder.Build();

// ===== Seed database with roles =====

// Creates a temporary Dependency Injection scope.
// A scope is used so that we can fetch scoped services (Identity/EF Core services)
// from the DI container of the application
using (var scope = app.Services.CreateScope())
{
    // Calls DbSeeder and passes the ServiceProvider of the scope.
    // SeedAsync then checks whether the roles exist, and creates them if they are missing.
    await DbSeeder.SeedAsync(scope.ServiceProvider, builder.Configuration);

}


app.UseAuthentication();
app.UseAuthorization();


// ===== Endpoints =====
// Login (the cookie is issued on success)
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
// Creates a POST endpoint for logout.
app.MapPost("/logout", async (SignInManager<AppUser> signInManager) =>
{
    // Signs the user out and removes the cookie auth
    await signInManager.SignOutAsync();
    return Results.Ok("Logget ud");
});



// protected GET endpoint where only users who satisfy the "CanAccessPassportData" policy get access.
// Passport data endpoint only available to Manager and Receptionist
// Creates a GET endpoint at the URL /admin/passportdata.
//Map = connect / map a URL to some code.
app.MapGet("/admin/passportdata", () =>
{
    return Results.Ok("Fake passport data");
})
    .RequireAuthorization("CanAccessPassportData");

// Protected endpoint for housekeeping data.
// Available to Manager, Receptionist and Housekeeping.
app.MapGet("/admin/housekeeping", () =>
{
    return Results.Ok("Fake housekeeping data");
})
.RequireAuthorization("CanAccessHousekeeping");


app.Run();
