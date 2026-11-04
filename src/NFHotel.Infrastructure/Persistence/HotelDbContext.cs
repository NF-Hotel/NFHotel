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
    /// Creates the context.
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

    /// <summary>The benchmark runs that the benchmark service delivered (ADR-0013).</summary>
    public DbSet<BenchmarkRun> BenchmarkRuns => Set<BenchmarkRun>();

    /// <summary>The records of the benchmark runs that a Manager started (ADR-0031).</summary>
    public DbSet<BenchmarkRequest> BenchmarkRequests => Set<BenchmarkRequest>();

    /// <summary>The default AI model per purpose (ADR-0014).</summary>
    public DbSet<AiDefault> AiDefaults => Set<AiDefault>();

    /// <summary>The service notes of the rooms.</summary>
    public DbSet<RoomServiceNote> RoomServiceNotes => Set<RoomServiceNote>();

    /// <summary>The housekeeper of each room (MIL-060).</summary>
    public DbSet<RoomAssignment> RoomAssignments => Set<RoomAssignment>();

    /// <summary>The trades of rooms between housekeepers (MIL-060).</summary>
    public DbSet<RoomTrade> RoomTrades => Set<RoomTrade>();

    /// <summary>The shifts (MIL-060).</summary>
    public DbSet<Shift> Shifts => Set<Shift>();

    /// <summary>The trades of shifts (MIL-060).</summary>
    public DbSet<ShiftTrade> ShiftTrades => Set<ShiftTrade>();

    /// <summary>The nightly base rate per room size (MIL-055).</summary>
    public DbSet<RoomRate> RoomRates => Set<RoomRate>();

    /// <summary>The price changes of the room sizes (MIL-055).</summary>
    public DbSet<PriceChange> PriceChanges => Set<PriceChange>();

    /// <summary>The one row of pricing settings (MIL-055).</summary>
    public DbSet<PricingSetting> PricingSettings => Set<PricingSetting>();

    /// <summary>The market fields of the bookings (MIL-026).</summary>
    public DbSet<BookingMarketDetail> BookingMarketDetails => Set<BookingMarketDetail>();

    /// <summary>The market analyses that the data analysis service delivered (MIL-026).</summary>
    public DbSet<MarketAnalysisRun> MarketAnalysisRuns => Set<MarketAnalysisRun>();

    /// <summary>The requests of the benchmark runs.</summary>
    public DbSet<BenchmarkResult> BenchmarkResults => Set<BenchmarkResult>();

    /// <summary>The audit trail of the staff accounts (ADR-0017).</summary>
    public DbSet<StaffAuditEvent> StaffAuditEvents => Set<StaffAuditEvent>();

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
        modelBuilder.ApplyConfiguration(new RoomServiceNoteConfiguration());
        modelBuilder.ApplyConfiguration(new RoomAssignmentConfiguration());
        modelBuilder.ApplyConfiguration(new RoomTradeConfiguration());
        modelBuilder.ApplyConfiguration(new ShiftConfiguration());
        modelBuilder.ApplyConfiguration(new ShiftTradeConfiguration());
        modelBuilder.ApplyConfiguration(new StaffConfiguration());
        modelBuilder.ApplyConfiguration(new BenchmarkRunConfiguration());
        modelBuilder.ApplyConfiguration(new BenchmarkRequestConfiguration());
        modelBuilder.ApplyConfiguration(new AiDefaultConfiguration());
        modelBuilder.ApplyConfiguration(new RoomRateConfiguration());
        modelBuilder.ApplyConfiguration(new PricingSettingConfiguration());
        modelBuilder.ApplyConfiguration(new PriceChangeConfiguration());
        modelBuilder.ApplyConfiguration(new MarketAnalysisRunConfiguration());
        modelBuilder.ApplyConfiguration(new BookingMarketDetailConfiguration());
        modelBuilder.ApplyConfiguration(new BenchmarkResultConfiguration());
        modelBuilder.ApplyConfiguration(new StaffAuditEventConfiguration());

        // GuestConfiguration is registered explicitly and not via assembly scanning, because it
        // has a dependency: the value converter that encrypts the passport number (B-09).
        modelBuilder.ApplyConfiguration(new GuestConfiguration(_passportEncryptor));
    }
}
