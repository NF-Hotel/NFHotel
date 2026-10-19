using System;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SecurityPrototype.Data;


// Database context for application.
// Inherits from IdentityDbContext, so it can use ASP.NET Core Identity with AppUser.
public class ApplicationDbContext : IdentityDbContext<AppUser>
{

    // Receives the database configuration that is set up in Program.cs.
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

}
