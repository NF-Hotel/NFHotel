using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Persistence;
using NFHotel.Infrastructure.Repositories;

namespace NFHotel.Infrastructure.Tests.Integration.Persistence;

/// <summary>
/// The count per email of the limit of the public booking against a real PostgreSQL (MIL-087 criterion 3, ADR-0051
/// decision 3): the query must translate to SQL and count the coming bookings made on the public site for the email,
/// compared without letter case, and nothing else; the mark is stored and read back.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class BookingPublicCountTests : IAsyncLifetime
{
    private static readonly DateOnly Today = new(2030, 1, 1);

    private readonly PostgreSqlFixture _fixture;
    private int _rooms;

    public BookingPublicCountTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Runs before each test and starts it on an empty database.</summary>
    public async ValueTask InitializeAsync() => await _fixture.ResetDatabaseAsync();

    /// <summary>Nothing to clean up: the next test resets the database.</summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    /// <summary>
    /// Integration: of a coming public booking in other letter case, a public booking whose departure is today, one whose
    /// departure was yesterday, a cancelled one, a coming one by staff and a coming one of another email, the first two
    /// count.
    /// </summary>
    [Fact]
    public async Task CountComingPublicBookingsAsync_MixedBookings_CountsTheComingPublicOnesOfTheEmail()
    {
        // Arrange
        await SeedAsync("Anna@Example.com", Today.AddDays(10), isPublic: true);
        await SeedAsync("anna@example.com", Today.AddDays(-2), isPublic: true);
        await SeedAsync("anna@example.com", Today.AddDays(-3), isPublic: true);
        await SeedAsync("anna@example.com", Today.AddDays(20), isPublic: true, cancelled: true);
        await SeedAsync("anna@example.com", Today.AddDays(30), isPublic: false);
        await SeedAsync("eve@example.com", Today.AddDays(40), isPublic: true);
        await using HotelDbContext context = _fixture.CreateContext();

        // Act
        int count = await new BookingRepository(context).CountComingPublicBookingsAsync("anna@example.com", Today, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, count);
    }

    /// <summary>
    /// Security (review RC-428 F12): the email is matched as itself and never as a pattern of the database: an underscore
    /// or a percent sign in it matches only itself, not another character or a run of them.
    /// </summary>
    [Fact]
    public async Task CountComingPublicBookingsAsync_EmailWithAPatternCharacter_MatchesOnlyItself()
    {
        // Arrange
        await SeedAsync("axb@example.com", Today.AddDays(10), isPublic: true);
        await SeedAsync("abcd@example.com", Today.AddDays(20), isPublic: true);
        await SeedAsync("a_b@example.com", Today.AddDays(30), isPublic: true);
        await using HotelDbContext context = _fixture.CreateContext();
        BookingRepository bookings = new(context);

        // Act
        int underscore = await bookings.CountComingPublicBookingsAsync("a_b@example.com", Today, TestContext.Current.CancellationToken);
        int percent = await bookings.CountComingPublicBookingsAsync("a%d@example.com", Today, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, underscore);
        Assert.Equal(0, percent);
    }

    /// <summary>
    /// Integration (review RC-428 F12, MIL-086 decision 4): the email is counted in its normal form, so spaces around it
    /// and another letter case count the bookings of the same email.
    /// </summary>
    [Fact]
    public async Task CountComingPublicBookingsAsync_EmailWithSpacesAndCapitals_CountsTheSameEmail()
    {
        // Arrange
        await SeedAsync("anna@example.com", Today.AddDays(10), isPublic: true);
        await using HotelDbContext context = _fixture.CreateContext();

        // Act
        int count = await new BookingRepository(context).CountComingPublicBookingsAsync("  ANNA@example.com ", Today, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, count);
    }

    /// <summary>Integration: the mark of a public booking is stored, and a booking without it is read back unmarked.</summary>
    [Fact]
    public async Task Add_PublicAndStaffBookings_StoresTheMark()
    {
        // Arrange
        int publicBooking = await SeedAsync("anna@example.com", Today.AddDays(10), isPublic: true);
        int staffBooking = await SeedAsync("anna@example.com", Today.AddDays(20), isPublic: false);
        await using HotelDbContext context = _fixture.CreateContext();
        BookingRepository bookings = new(context);

        // Act
        Booking? readPublic = await bookings.GetByIdAsync(publicBooking, TestContext.Current.CancellationToken);
        Booking? readStaff = await bookings.GetByIdAsync(staffBooking, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(readPublic?.MadeOnPublicSite);
        Assert.False(readStaff?.MadeOnPublicSite);
    }

    // A room of its own, a guest with the email and a booking of two nights from the start, made as if today were ten days
    // before it, so a booking in the past can be stored.
    private async Task<int> SeedAsync(string email, DateOnly start, bool isPublic, bool cancelled = false)
    {
        await using HotelDbContext context = _fixture.CreateContext();
        Room room = Room.Create($"{++_rooms + 100}", 1, RoomSize.Double, 2);
        Guest guest = Guest.Create("Anna", "Jensen", email, "+4512345678", "Denmark", null);
        new RoomRepository(context).Add(room);
        new GuestRepository(context).Add(guest);
        await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
        Booking booking = Booking.Create(new DateRange(start, start.AddDays(2)), room.Id, guest.Id, start.AddDays(-10));
        if (isPublic)
        {
            booking.MarkMadeOnPublicSite();
        }

        if (cancelled)
        {
            booking.Cancel();
        }

        new BookingRepository(context).Add(booking);
        await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
        return booking.Id;
    }
}
