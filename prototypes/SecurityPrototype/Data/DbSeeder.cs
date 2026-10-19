using System;

using Microsoft.AspNetCore.Identity;

using SecurityPrototype.Security;

namespace SecurityPrototype.Data;

// Class used to insert the start data into the database.
public static class DbSeeder
{
    // Asynchronous method that creates the necessary roles in the database.
    // IServiceProvider gives access to the services that are registered in DI.
    // IConfiguration is an interface from ASP.NET Core that gives access to the configuration of the application
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {

        // Retrieves RoleManager from Dependency Injection.
        // RoleManager is used for roles in ASP.NET Identity.
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // String array with the roles that the system must have.
        // The values come from our Roles class.
        string[] roles =

        {
            Roles.Manager,
            Roles.Receptionist,
            Roles.Housekeeping

        };

        // Foreach loop that goes through the roles one at a time
        foreach (var role in roles)
        {
            // If statement that checks whether the role already exists in the database.
            if (!await roleManager.RoleExistsAsync(role))
            {

                // If the role does not exist, it is created here.
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }


        //
        // Fetches UserManager<AppUser> from the Dependency Injection container.
        // UserManager is an ASP.NET Core Identity service that is used to create, find and manage users.
        var userManager = services.GetRequiredService<UserManager<AppUser>>();

        // Fetches the test passwords from User Secrets
        // The passwords are in User Secrets, so they are not pushed to Git/GitHub.
        var managerPassword = configuration["SeedUsers:ManagerPassword"];
        var receptionistPassword = configuration["SeedUsers:ReceptionistPassword"];
        var housekeepingPassword = configuration["SeedUsers:HousekeepingPassword"];

        // Stops the program if one or more passwords are missing.
        if (string.IsNullOrWhiteSpace(managerPassword) || string.IsNullOrWhiteSpace(receptionistPassword) || string.IsNullOrWhiteSpace(housekeepingPassword))
        {
            throw new InvalidOperationException("One or more seed passwords are not configured.");
        }

        // Calls the Ensure method to create users with the given roles.
        async Task EnsureUser(string email, string password, string role)
        {
            // Checks whether the user already exists in the database.
            var existingUser = await userManager.FindByEmailAsync(email);

            if (existingUser is null)
            {
                // Creates a new user if there is no existing one.
                var user = new AppUser
                {
                    UserName = email,
                    Email = email
                };


                // Creates the user with the password from User Secrets.
                var result = await userManager.CreateAsync(user, password);


                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Could not create seed user {email}.");
                }

                // Adds the user to the given role.
                await userManager.AddToRoleAsync(user, role);
            }

        }

        // Creates the three fake test users if they do not already exist.
        await EnsureUser("reception@nfhotel.test", receptionistPassword, Roles.Receptionist);
        await EnsureUser("housekeeping@nfhotel.test", housekeepingPassword, Roles.Housekeeping);
        await EnsureUser("manager@nfhotel.test", managerPassword, Roles.Manager);


    }


}
