using Microsoft.EntityFrameworkCore;

using NFHotel.Domain.Entities;
using NFHotel.Infrastructure.Persistence.Configurations;
using NFHotel.Infrastructure.Security;

namespace NFHotel.Infrastructure.Persistence;

/// <summary>
/// EF Core-konteksten for hoteldatabasen.
/// </summary>
/// <remarks>
/// Konteksten kender ikke forretningsregler — den mapper kun. Providervalg og
/// navnekonvention (<c>UseSnakeCaseNamingConvention</c>, B-06) sættes ved registreringen i
/// <see cref="DependencyInjection.AddInfrastructure"/>, ikke her, så samme kontekst kan
/// bruges mod en Testcontainers-database uden en anden kodesti.
/// <para>
/// Lazy loading er slået fra. Navigationer hentes med eksplicit <c>Include</c> på
/// kommandostier og projiceres direkte i SQL på læsestier (A-11).
/// </para>
/// </remarks>
public sealed class HotelDbContext : DbContext
{
    private readonly IStringEncryptor _passportEncryptor;

    /// <summary>
    /// Opretter konteksten.
    /// </summary>
    /// <param name="options">Kontekstindstillingerne, herunder provider og connection string.</param>
    /// <param name="passportEncryptor">
    /// Krypteringstjenesten for pasnummer (B-09). Injiceres frem for at blive slået op
    /// statisk, så en test kan sætte sin egen nøgle uden at røde global tilstand.
    /// </param>
    /// <exception cref="ArgumentNullException">Kastes hvis <paramref name="passportEncryptor"/> er <c>null</c>.</exception>
    public HotelDbContext(DbContextOptions<HotelDbContext> options, IStringEncryptor passportEncryptor)
        : base(options)
    {
        ArgumentNullException.ThrowIfNull(passportEncryptor);

        _passportEncryptor = passportEncryptor;
    }

    /// <summary>Bookinger.</summary>
    public DbSet<Booking> Bookings => Set<Booking>();

    /// <summary>Rum.</summary>
    public DbSet<Room> Rooms => Set<Room>();

    /// <summary>Gæster.</summary>
    public DbSet<Guest> Guests => Set<Guest>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        // btree_gist kræves af exclusion constrainten, fordi den blander btree-operatoren
        // '=' på room_id med gist-operatoren '&&' på daterange (B-03).
        modelBuilder.HasPostgresExtension(BookingOverlapConstraint.RequiredExtension);

        modelBuilder.ApplyConfiguration(new BookingConfiguration());
        modelBuilder.ApplyConfiguration(new RoomConfiguration());

        // GuestConfiguration registreres eksplicit og ikke via assembly-scanning, fordi den
        // har en afhængighed: value converteren der krypterer pasnummer (B-09).
        modelBuilder.ApplyConfiguration(new GuestConfiguration(_passportEncryptor));
    }
}