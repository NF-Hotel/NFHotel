using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Persistence;
using NFHotel.Infrastructure.Repositories;
using NFHotel.TestSupport;

namespace NFHotel.Infrastructure.Tests.Integration.Persistence;

/// <summary>
/// The read side of guests and rooms against a real PostgreSQL: lists, searches, filters and the existence checks
/// must translate to SQL and return what was stored.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class GuestAndRoomReadTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;

    public GuestAndRoomReadTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Runs before each test and starts it on an empty database.</summary>
    public async ValueTask InitializeAsync() => await _fixture.ResetDatabaseAsync();

    /// <summary>Nothing to clean up: the next test resets the database.</summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    #region Guests

    /// <summary>Integration and positive: the details of a stored guest show the passport number decrypted.</summary>
    [Fact]
    public async Task GuestGetDetailsAsync_StoredGuest_ReturnsTheDetailsWithThePassportNumber()
    {
        // Arrange
        var guestId = await SeedGuestAsync("Anna", "Jensen", "PASS12345");
        await using var context = _fixture.CreateContext();

        // Act
        var details = await new GuestRepository(context).GetDetailsAsync(guestId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("PASS12345", details?.PassportNumber);
    }

    /// <summary>Negative: an unknown guest has no details.</summary>
    [Fact]
    public async Task GuestGetDetailsAsync_UnknownGuest_ReturnsNull()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var details = await new GuestRepository(context).GetDetailsAsync(999, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(details);
    }

    /// <summary>Equivalence: every search term must match some field, in any case; no term lists everyone.</summary>
    [Fact]
    public async Task GuestSearchAsync_TermsAndNoTerms_FiltersOnEveryTerm()
    {
        // Arrange
        await SeedGuestAsync("Anna", "Jensen", null);
        await SeedGuestAsync("Bo", "Jensen", null);
        await using var context = _fixture.CreateContext();
        var repository = new GuestRepository(context);

        // Act
        var everyone = await repository.SearchAsync([], TestContext.Current.CancellationToken);
        var onlyAnna = await repository.SearchAsync(["JENSEN", "anna"], TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, everyone.Count);
        Assert.Equal("Anna", Assert.Single(onlyAnna).FirstName);
    }

    /// <summary>Edge: a search term with the characters of a SQL pattern is taken literally.</summary>
    [Fact]
    public async Task GuestSearchAsync_PercentSign_IsTakenLiterally()
    {
        // Arrange
        await SeedGuestAsync("Anna", "Jensen", null);
        await using var context = _fixture.CreateContext();

        // Act
        var found = await new GuestRepository(context).SearchAsync(["%"], TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(found);
    }

    /// <summary>
    /// Edge (MIL-086 decision 5): the two characters of a SQL pattern in an email are found as themselves, so a search
    /// for one of them finds the guest whose email holds it and no one else.
    /// </summary>
    /// <param name="term">The character that is searched for.</param>
    /// <param name="expectedEmail">The only email that holds it.</param>
    [Theory]
    [InlineData("%", "anna%sale@example.com")]
    [InlineData("_", "bo_jensen@example.com")]
    public async Task GuestSearchAsync_WildcardInAnEmail_FindsThatGuestOnly(string term, string expectedEmail)
    {
        // Arrange
        await SeedGuestAsync("Anna", "Jensen", null, "anna%sale@example.com");
        await SeedGuestAsync("Bo", "Jensen", null, "bo_jensen@example.com");
        await SeedGuestAsync("Carl", "Jensen", null, "carl@example.com");
        await using var context = _fixture.CreateContext();

        // Act
        var found = await new GuestRepository(context).SearchAsync([term], TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expectedEmail, Assert.Single(found).Email);
    }

    /// <summary>
    /// Integration (MIL-086 decision 5): each hostile string that meets the rule of a field of the guest is stored and
    /// read back unchanged, character for character; the others are refused by the rule before anything is stored.
    /// </summary>
    [Fact]
    public async Task Guest_HostileStringsThatMeetTheRules_AreStoredAndReadUnchanged()
    {
        // Arrange
        var stored = new List<(int GuestId, string Field, string Value)>();
        foreach (var (_, value) in HostileStrings.All)
        {
            foreach (var field in GuestFieldNames)
            {
                Guest guest;
                try
                {
                    guest = CreateGuestWith(field, value);
                }
                catch (DomainException)
                {
                    continue;
                }

                await using var seedContext = _fixture.CreateContext();
                new GuestRepository(seedContext).Add(guest);
                await new UnitOfWork(seedContext).SaveChangesAsync(TestContext.Current.CancellationToken);
                stored.Add((guest.Id, field, value));
            }
        }

        await using var context = _fixture.CreateContext();
        var repository = new GuestRepository(context);

        // Act
        var readBack = new List<(string Field, string Value, string? Read)>();
        foreach (var (guestId, field, value) in stored)
        {
            var details = await repository.GetDetailsAsync(guestId, TestContext.Current.CancellationToken);
            readBack.Add((field, value, FieldOf(details, field)));
        }

        // Assert
        Assert.NotEmpty(stored);
        Assert.All(readBack, item => Assert.Equal(item.Value, item.Read));
    }

    /// <summary>Integration: the existence and booking checks answer from the database.</summary>
    [Fact]
    public async Task GuestExistsAndHasAnyBookingsAsync_StoredGuestWithoutBookings_AnswerTrueAndFalse()
    {
        // Arrange
        var guestId = await SeedGuestAsync("Anna", "Jensen", null);
        await using var context = _fixture.CreateContext();
        var repository = new GuestRepository(context);

        // Act
        var exists = await repository.ExistsAsync(guestId, TestContext.Current.CancellationToken);
        var missing = await repository.ExistsAsync(999, TestContext.Current.CancellationToken);
        var hasBookings = await repository.HasAnyBookingsAsync(guestId, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(exists);
        Assert.False(missing);
        Assert.False(hasBookings);
    }

    #endregion

    #region Rooms

    /// <summary>Integration and positive: the details, the list and the floors of stored rooms.</summary>
    [Fact]
    public async Task RoomReads_StoredRooms_ReturnDetailsListAndFloors()
    {
        // Arrange
        var first = await SeedRoomAsync("101", 1, RoomSize.Double);
        await SeedRoomAsync("201", 2, RoomSize.Suite);
        await using var context = _fixture.CreateContext();
        var repository = new RoomRepository(context);

        // Act
        var details = await repository.GetDetailsAsync(first, TestContext.Current.CancellationToken);
        var all = await repository.GetAllAsync(TestContext.Current.CancellationToken);
        var floors = await repository.GetDistinctFloorsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("101", details?.RoomNumber);
        Assert.Equal(2, all.Count);
        Assert.Equal([1, 2], floors);
    }

    /// <summary>Equivalence: the filter narrows on each field it is given and ignores the ones it is not.</summary>
    [Fact]
    public async Task RoomGetByFilterAsync_FloorAndSize_NarrowsTheList()
    {
        // Arrange
        await SeedRoomAsync("101", 1, RoomSize.Double);
        await SeedRoomAsync("102", 1, RoomSize.Suite);
        await SeedRoomAsync("201", 2, RoomSize.Suite);
        await using var context = _fixture.CreateContext();
        var repository = new RoomRepository(context);

        // Act
        var floorOne = await repository.GetByFilterAsync(new RoomFilter(Floor: 1), TestContext.Current.CancellationToken);
        var suitesOnOne = await repository.GetByFilterAsync(new RoomFilter(Floor: 1, Size: RoomSize.Suite), TestContext.Current.CancellationToken);
        var nobody = await repository.GetByFilterAsync(new RoomFilter(Floor: 9), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, floorOne.Count);
        Assert.Equal("102", Assert.Single(suitesOnOne).RoomNumber);
        Assert.Empty(nobody);
    }

    /// <summary>Integration: only rooms with the asked status are offered for letting.</summary>
    [Fact]
    public async Task RoomGetByStatusAsync_Available_ReturnsOnlyAvailableRooms()
    {
        // Arrange
        await SeedRoomAsync("101", 1, RoomSize.Double);
        var blocked = await SeedRoomAsync("102", 1, RoomSize.Double, outOfService: true);
        await using var context = _fixture.CreateContext();

        // Act
        var available = await new RoomRepository(context).GetByStatusAsync(RoomStatus.Available, TestContext.Current.CancellationToken);

        // Assert
        Assert.DoesNotContain(available, room => room.RoomId == blocked);
        Assert.Single(available);
    }

    /// <summary>Integration: the number check excludes the room being edited, so a room keeps its own number.</summary>
    [Fact]
    public async Task RoomNumberExistsAsync_OwnNumberAndOtherNumber_ExcludesTheRoomItself()
    {
        // Arrange
        var id = await SeedRoomAsync("101", 1, RoomSize.Double);
        await using var context = _fixture.CreateContext();
        var repository = new RoomRepository(context);

        // Act
        var taken = await repository.RoomNumberExistsAsync("101", null, TestContext.Current.CancellationToken);
        var ownNumber = await repository.RoomNumberExistsAsync("101", id, TestContext.Current.CancellationToken);
        var free = await repository.RoomNumberExistsAsync("999", null, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(taken);
        Assert.False(ownNumber);
        Assert.False(free);
    }

    #endregion

    #region Helpers

    private static readonly string[] GuestFieldNames = ["firstName", "lastName", "email", "phoneNumber", "country", "passportNumber"];

    private async Task<int> SeedGuestAsync(string firstName, string lastName, string? passportNumber, string? email = null)
    {
        await using var context = _fixture.CreateContext();
        var guest = Guest.Create(firstName, lastName, email ?? $"{firstName.ToLowerInvariant()}@example.com", "+4512345678", "Denmark", passportNumber);
        new GuestRepository(context).Add(guest);
        await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
        return guest.Id;
    }

    // A guest whose one field holds the value, and plausible values in the others.
    private static Guest CreateGuestWith(string field, string value) => Guest.Create(
        field == "firstName" ? value : "Anna",
        field == "lastName" ? value : "Jensen",
        field == "email" ? value : "anna@example.com",
        field == "phoneNumber" ? value : "+4512345678",
        field == "country" ? value : "Denmark",
        field == "passportNumber" ? value : null);

    private static string? FieldOf(GuestDetailsDto? details, string field) => field switch
    {
        "firstName" => details?.FirstName,
        "lastName" => details?.LastName,
        "email" => details?.Email,
        "phoneNumber" => details?.PhoneNumber,
        "country" => details?.Country,
        "passportNumber" => details?.PassportNumber,
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Not a field of the guest."),
    };

    private async Task<int> SeedRoomAsync(string number, int floor, RoomSize size, bool outOfService = false)
    {
        await using var context = _fixture.CreateContext();
        var room = Room.Create(number, floor, size, 2);
        if (outOfService)
        {
            room.TakeOutOfService();
        }

        new RoomRepository(context).Add(room);
        await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
        return room.Id;
    }

    #endregion
}
