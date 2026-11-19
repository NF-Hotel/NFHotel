using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

using NSubstitute;

namespace NFHotel.Application.Tests.Services;

/// <summary>
/// The limit per email of the public booking (MIL-087 criterion 3, ADR-0051 decision 3): at most the number of
/// <c>ISenderLimits.ComingPublicBookingsPerEmail</c> coming bookings made on the public site for one email, where coming
/// means not cancelled and with the departure today or later; a booking by staff is neither limited nor counted, and every
/// public booking is marked as one.
/// </summary>
public sealed class PublicBookingLimitTests
{
    private const string Email = "anna@example.com";

    #region The mark

    /// <summary>Positive: a booking made on the public site is marked as one, and a booking by staff is not.</summary>
    [Fact]
    public async Task Create_PublicAndByStaff_MarksOnlyThePublicBooking()
    {
        // Arrange
        ServiceWorld world = World();
        Room room = world.Rooms.Items.First();

        // Act
        Result<BookingDetailsDto> made = await world.BookingService().CreatePublicAsync(PublicRequest(world, 0, Email), Token);
        Result<BookingDetailsDto> byStaff = await StaffService(world).CreateAsync(
            new CreateBookingRequest(world.Clock.Today.AddDays(40), world.Clock.Today.AddDays(42), room.Id, null, Guest(Email)),
            Token);

        // Assert
        Assert.True(made.IsSuccess);
        Assert.True(byStaff.IsSuccess);
        Assert.True(BookingOf(world, made.Value.BookingId).MadeOnPublicSite);
        Assert.False(BookingOf(world, byStaff.Value.BookingId).MadeOnPublicSite);
    }

    #endregion

    #region The limit

    /// <summary>
    /// Boundary: three coming public bookings of one email are made; the fourth, with the email in other letter case and
    /// with spaces around it, is refused with the code of the limits and stores neither a guest nor a booking.
    /// </summary>
    [Fact]
    public async Task CreatePublicAsync_FourthComingBookingOfOneEmail_IsRefused()
    {
        // Arrange
        ServiceWorld world = World();
        bool[] made = await BookAsync(world, 0, 3, Email);
        int guests = world.Guests.Items.Count;

        // Act
        Result<BookingDetailsDto> refused = await world.BookingService().CreatePublicAsync(PublicRequest(world, 3, " ANNA@Example.com "), Token);

        // Assert
        Assert.Equal([true, true, true], made);
        Assert.Equal([ErrorCodes.Booking.TooManyRequests], refused.Errors.Select(error => error.Code));
        Assert.Equal(guests, world.Guests.Items.Count);
        Assert.Equal(3, world.Bookings.Items.Count);
    }

    /// <summary>Positive: the number comes from the limits of the settings, not from the code of the use case.</summary>
    [Fact]
    public async Task CreatePublicAsync_LimitOfOne_RefusesTheSecond()
    {
        // Arrange
        ServiceWorld world = World();
        world.SenderLimits.ComingPublicBookingsPerEmail.Returns(1);

        // Act
        bool[] made = await BookAsync(world, 0, 2, Email);

        // Assert
        Assert.Equal([true, false], made);
    }

    /// <summary>Equivalence: the bookings of other emails do not count.</summary>
    [Fact]
    public async Task CreatePublicAsync_OtherEmails_DoNotCount()
    {
        // Arrange
        ServiceWorld world = World();
        await BookAsync(world, 0, 3, "eve@example.com");

        // Act
        bool[] made = await BookAsync(world, 3, 3, Email);

        // Assert
        Assert.Equal([true, true, true], made);
    }

    /// <summary>Equivalence: a cancelled booking of the email does not count, so the guest who cancels can book again.</summary>
    [Fact]
    public async Task CreatePublicAsync_ACancelledBookingOfTheEmail_DoesNotCount()
    {
        // Arrange
        ServiceWorld world = World();
        await BookAsync(world, 0, 3, Email);
        world.Bookings.Items[0].Cancel();

        // Act
        Result<BookingDetailsDto> made = await world.BookingService().CreatePublicAsync(PublicRequest(world, 3, Email), Token);

        // Assert
        Assert.True(made.IsSuccess);
    }

    /// <summary>
    /// Boundary: a booking whose departure is today still counts; once its departure is yesterday it does not, so the
    /// count follows the day of the hotel.
    /// </summary>
    [Fact]
    public async Task CreatePublicAsync_ABookingWhoseDepartureHasPassed_DoesNotCount()
    {
        // Arrange
        ServiceWorld world = World();
        await BookAsync(world, 0, 3, Email);
        DateOnly departure = world.Bookings.Items[0].EndDate;
        world.Clock.Today = departure;
        Result<BookingDetailsDto> onTheDayOfDeparture = await world.BookingService().CreatePublicAsync(PublicRequest(world, 30, Email), Token);

        // Act
        world.Clock.Today = departure.AddDays(1);
        Result<BookingDetailsDto> dayAfter = await world.BookingService().CreatePublicAsync(PublicRequest(world, 31, Email), Token);

        // Assert
        Assert.Equal([ErrorCodes.Booking.TooManyRequests], onTheDayOfDeparture.Errors.Select(error => error.Code));
        Assert.True(dayAfter.IsSuccess);
    }

    /// <summary>Equivalence: staff can always book, also for an email whose public bookings are at the limit, and their bookings do not count.</summary>
    [Fact]
    public async Task Create_ByStaff_IsNeitherLimitedNorCounted()
    {
        // Arrange
        ServiceWorld world = World();
        Room room = world.Rooms.Items.First();
        await BookAsync(world, 0, 2, Email);
        Result<BookingDetailsDto> byStaff = await StaffService(world).CreateAsync(
            new CreateBookingRequest(world.Clock.Today.AddDays(40), world.Clock.Today.AddDays(42), room.Id, null, Guest(Email)),
            Token);

        // Act
        Result<BookingDetailsDto> third = await world.BookingService().CreatePublicAsync(PublicRequest(world, 2, Email), Token);
        Result<BookingDetailsDto> byStaffAtTheLimit = await StaffService(world).CreateAsync(
            new CreateBookingRequest(world.Clock.Today.AddDays(50), world.Clock.Today.AddDays(52), room.Id, null, Guest(Email)),
            Token);

        // Assert
        Assert.True(byStaff.IsSuccess);
        Assert.True(third.IsSuccess);
        Assert.True(byStaffAtTheLimit.IsSuccess);
    }

    /// <summary>
    /// Equivalence (ADR-0051 decision 3): the count comes after the rules of the use case, so a stay that a rule refuses
    /// gets the code of that rule, not the code of the limits.
    /// </summary>
    [Fact]
    public async Task CreatePublicAsync_StayThatARuleRefusesForAnEmailAtTheLimit_GetsTheCodeOfTheRule()
    {
        // Arrange
        ServiceWorld world = World();
        await BookAsync(world, 0, 3, Email);
        CreatePublicBookingRequest pastStay = new(
            world.Clock.Today.AddDays(-1),
            world.Clock.Today.AddDays(1),
            RoomSize.Double,
            1,
            Guest(Email));

        // Act
        Result<BookingDetailsDto> refused = await world.BookingService().CreatePublicAsync(pastStay, Token);

        // Assert
        Assert.Equal([ErrorCodes.Booking.StartDateInPast], refused.Errors.Select(error => error.Code));
    }

    #endregion

    #region Helpers

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    // Four double rooms that sleep two, so every stay of a test finds a free room.
    private static ServiceWorld World()
    {
        ServiceWorld world = new();
        for (int number = 0; number < 4; number++)
        {
            world.AddRoom($"10{number + 1}", 1, RoomSize.Double, 2);
        }

        return world;
    }

    private static GuestFields Guest(string email) => new("Anna", "Jensen", email, "+4512345678", "Denmark", null);

    // A stay of its own for each number, three days after the one before, from five days ahead.
    private static CreatePublicBookingRequest PublicRequest(ServiceWorld world, int stay, string email)
    {
        DateOnly checkIn = world.Clock.Today.AddDays(5 + (3 * stay));
        return new CreatePublicBookingRequest(checkIn, checkIn.AddDays(2), RoomSize.Double, 1, Guest(email));
    }

    private static async Task<bool[]> BookAsync(ServiceWorld world, int firstStay, int count, string email)
    {
        List<bool> made = [];
        for (int stay = firstStay; stay < firstStay + count; stay++)
        {
            Result<BookingDetailsDto> result = await world.BookingService().CreatePublicAsync(PublicRequest(world, stay, email), Token);
            made.Add(result.IsSuccess);
        }

        return [.. made];
    }

    private static Booking BookingOf(ServiceWorld world, int bookingId) =>
        world.Bookings.Items.Single(booking => booking.Id == bookingId);

    // The service as a member of staff who may create bookings.
    private static BookingService StaffService(ServiceWorld world)
    {
        IStaffAuthorizationService authorization = Substitute.For<IStaffAuthorizationService>();
        authorization.HasPermissionAsync(Permissions.CreateBooking, Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        return world.BookingService(authorization);
    }

    #endregion
}
