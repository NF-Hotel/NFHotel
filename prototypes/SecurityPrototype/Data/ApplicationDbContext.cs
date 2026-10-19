using System;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SecurityPrototype.Data;


// Database context for application.
// Inherits fra IdentityDbContext, så den kan bruge ASP.NET Core Identity med AppUser.
public class ApplicationDbContext : IdentityDbContext<AppUser>
{

    // Modtager databasekonfigurationen, som er opsat i Program.cs.
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

}
