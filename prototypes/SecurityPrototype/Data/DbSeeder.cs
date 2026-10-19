using System;

using Microsoft.AspNetCore.Identity;

using SecurityPrototype.Security;

namespace SecurityPrototype.Data;

// Klasse der bruges til at indsætte startdata i databasen.
public static class DbSeeder
{
    // Asynkron metode der opretter de nødvendige roller i databasen.
    // IServiceProvider giver adgang til de services, der er registreret i DI.
    // IConfiguration er et interface fra ASP.NET Core, som giver adgang til applikationens konfiguration
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {

        // Retriver RoleManager fra Dependency Injection.
        // RoleManager bruges til roller i ASP.NET Identity.
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // String-Array med de roller systemet skal have.
        // Værdierne kommer fra vores Roles-klassen.
        string[] roles =

        {
            Roles.Manager,
            Roles.Receptionist,
            Roles.Housekeeping

        };

        // Foreach loop der går igennem rollerne en ad gange
        foreach (var role in roles)
        {
            // If statement der kontrollerer om rollen allerede findes i databasen.
            if (!await roleManager.RoleExistsAsync(role))
            {

                // Hvis rolle ikke findes. Oprettets den her.
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }


        //
        // Henter UserManager<AppUser> fra Dependency Injection-containeren.
        // UserManager er en ASP.NET Core Identity-service, som bruges til a oprette, finde og administrere brugere.
        var userManager = services.GetRequiredService<UserManager<AppUser>>();

        // Henter test-passwords fra User Secrets
        // Passwords ligger i User Secrets, så de ik blive pushed til Git/GitHub.
        var managerPassword = configuration["SeedUsers:ManagerPassword"];
        var receptionistPassword = configuration["SeedUsers:ReceptionistPassword"];
        var housekeepingPassword = configuration["SeedUsers:HousekeepingPassword"];

        // Stopper programmet hvis et eller flere passwords mangler.
        if (string.IsNullOrWhiteSpace(managerPassword) || string.IsNullOrWhiteSpace(receptionistPassword) || string.IsNullOrWhiteSpace(housekeepingPassword))
        {
            throw new InvalidOperationException("One or more seed passwords are not configured.");
        }

        // Kalder Ensure-metoden for at oprette brugere med de angivne roller.
        async Task EnsureUser(string email, string password, string role)
        {
            // Tjekker om brugeren allerede findes i databasen.
            var existingUser = await userManager.FindByEmailAsync(email);

            if (existingUser is null)
            {
                // Opretter en ny bruger, hvis der ikke findes en eksisterende.
                var user = new AppUser
                {
                    UserName = email,
                    Email = email
                };


                // Opretter brugeren med password fra User Secrets.
                var result = await userManager.CreateAsync(user, password);


                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Could not create seed user {email}.");
                }

                // Tilføjer brugeren til den angivne rolle.
                await userManager.AddToRoleAsync(user, role);
            }

        }

        // Opretter de tre fake testbrugere, hvis de ikke allerede findes.
        await EnsureUser("reception@nfhotel.test", receptionistPassword, Roles.Receptionist);
        await EnsureUser("housekeeping@nfhotel.test", housekeepingPassword, Roles.Housekeeping);
        await EnsureUser("manager@nfhotel.test", managerPassword, Roles.Manager);


    }


}
