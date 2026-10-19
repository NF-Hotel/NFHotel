using Microsoft.EntityFrameworkCore;

using NFHotel.Domain.Entities;
using NFHotel.Infrastructure.Persistence.Configurations;
using NFHotel.Infrastructure.Security;

namespace NFHotel.Infrastructure.Persistence;

/// <summary>
/// EF Core-konteksten for hoteldatabasen.
/// </summary>
/// <remarks>
/// The context knows no business rules — it only maps. The choice of provider and
/// naming convention (<c>UseSnakeCaseNamingConvention</c>, B-06) is made at the registration in
/// <see cref="DependencyInjection.AddInfrastructure"/>, not here, so the same context can
/// be used against a Testcontainers database without a different code path.
/// <para>
/// Lazy loading is switched off. Navigations are fetched with an explicit <c>Include</c> on
/// command paths and projected directly in SQL on read paths (A-11).
/// </para>
/// </remarks>
public sealed class HotelDbContext : DbContext
{
    private readonly IStringEncryptor _passportEncryptor;

    /// <summary>
    /// Opretter konteksten.
    /// </summary>
    /// <param name="options">The context options, including provider and connection string.</param>
    /// <param name="passportEncryptor">
    /// The encryption service for the passport number (B-09). Injected rather than looked up
    /// statically, so a test can set its own key without stirring up global state.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="passportEncryptor"/> is <c>null</c>.</exception>
    public HotelDbContext(DbContextOptions<HotelDbContext> options, IStringEncryptor passportEncryptor)
        : base(options)
    {
        ArgumentNullException.ThrowIfNull(passportEncryptor);

        _passportEncryptor = passportEncryptor;
    }

    /// <summary>Bookings.</summary>
    public DbSet<Booking> Bookings => Set<Booking>();

    /// <summary>Rooms.</summary>
    public DbSet<Room> Rooms => Set<Room>();

    /// <summary>Guests.</summary>
    public DbSet<Guest> Guests => Set<Guest>();

    /// <summary>Staff accounts (ADR-0005).</summary>
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();

    /// <summary>The benchmark runs that the benchmark service delivered (ADR-0008).</summary>
    public DbSet<BenchmarkRun> BenchmarkRuns => Set<BenchmarkRun>();

    /// <summary>The requests of the benchmark runs.</summary>
    public DbSet<BenchmarkResult> BenchmarkResults => Set<BenchmarkResult>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        // btree_gist is required by the exclusion constraint, because it mixes the btree operator
        // '=' on room_id with the gist operator '&&' on daterange (B-03).
        modelBuilder.HasPostgresExtension(BookingOverlapConstraint.RequiredExtension);

        modelBuilder.ApplyConfiguration(new BookingConfiguration());
        modelBuilder.ApplyConfiguration(new RoomConfiguration());
        modelBuilder.ApplyConfiguration(new StaffConfiguration());
        modelBuilder.ApplyConfiguration(new BenchmarkRunConfiguration());
        modelBuilder.ApplyConfiguration(new BenchmarkResultConfiguration());

        // GuestConfiguration is registered explicitly and not via assembly scanning, because it
        // has a dependency: the value converter that encrypts the passport number (B-09).
        modelBuilder.ApplyConfiguration(new GuestConfiguration(_passportEncryptor));
    }
}