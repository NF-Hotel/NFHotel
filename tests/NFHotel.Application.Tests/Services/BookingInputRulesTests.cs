using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

using NSubstitute;

namespace NFHotel.Application.Tests.Services;

/// <summary>
/// The rules of the booking input in the use cases (MIL-086 decisions 2 and 3, ADR-0050, BR-N-08, BR-C-07), with a
/// fixed clock (today is 2026-01-01, so the five years end on 2031-01-01): the public booking, the booking and the move
/// by staff, the move by a guest and the search for free rooms. A refused request stores nothing, a new guest included.
/// </summary>
public sealed class BookingInputRulesTests
{
    private static readonly DateOnly LastDay = new(2031, 1, 1);

    #region The public booking

    /// <summary>Boundary: a party of 1 and of 4 books; 0, 5, a negative party and the largest number are refused and store nothing.</summary>
    [Theory]
    [InlineData(1, true)]
    [InlineData(4, true)]
    [InlineData(0, false)]
    [InlineData(5, false)]
    [InlineData(-1, false)]
    [InlineData(int.MaxValue, false)]
    public async Task CreatePublicAsync_Party_IsOneToFour(int guests, bool accepted)
    {
        // Arrange
        ServiceWorld world = new();
        world.AddRoom("101", 1, RoomSize.Suite, 4);
        CreatePublicBookingRequest request = new(world.Clock.Today, world.Clock.Today.AddDays(2), RoomSize.Suite, guests, ServiceWorld.ValidGuestFields());

        // Act
        Result<BookingDetailsDto> result = await world.BookingService().CreatePublicAsync(request, TestContext.Current.CancellationToken);

        // Assert
        if (accepted)
        {
            Assert.True(result.IsSuccess);
        }
        else
        {
            Assert.Equal(ErrorCodes.Booking.GuestsOutOfRange, result.FirstError?.Code);
            AssertNothingStored(world);
        }
    }

    /// <summary>Boundary: on the public site a stay of 30 nights books and one of 31 is refused without a guest being stored.</summary>
    [Theory]
    [InlineData(30, true)]
    [InlineData(31, false)]
    public async Task CreatePublicAsync_Nights_AreAtMostThirty(int nights, bool accepted)
    {
        // Arrange
        ServiceWorld world = new();
        world.AddRoom();
        CreatePublicBookingRequest request = new(world.Clock.Today.AddDays(5), world.Clock.Today.AddDays(5 + nights), RoomSize.Double, 1, ServiceWorld.ValidGuestFields());

        // Act
        Result<BookingDetailsDto> result = await world.BookingService().CreatePublicAsync(request, TestContext.Current.CancellationToken);

        // Assert
        if (accepted)
        {
            Assert.True(result.IsSuccess);
        }
        else
        {
            Assert.Equal(ErrorCodes.Booking.StayTooLong, result.FirstError?.Code);
            AssertNothingStored(world);
        }
    }

    /// <summary>Boundary: a public stay that ends exactly five years after today books; a day later is refused.</summary>
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public async Task CreatePublicAsync_Departure_IsAtMostFiveYearsAhead(int daysBeyond, bool accepted)
    {
        // Arrange
        ServiceWorld world = new();
        world.AddRoom();
        DateOnly departure = LastDay.AddDays(daysBeyond);
        CreatePublicBookingRequest request = new(departure.AddDays(-3), departure, RoomSize.Double, 1, ServiceWorld.ValidGuestFields());

        // Act
        Result<BookingDetailsDto> result = await world.BookingService().CreatePublicAsync(request, TestContext.Current.CancellationToken);

        // Assert
        if (accepted)
        {
            Assert.True(result.IsSuccess);
        }
        else
        {
            Assert.Equal(ErrorCodes.Booking.EndDateTooFarAhead, result.FirstError?.Code);
            AssertNothingStored(world);
        }
    }

    /// <summary>Negative: an arrival yesterday is refused before the new guest is stored, so no guest is left without a booking.</summary>
    [Fact]
    public async Task CreatePublicAsync_ArrivalYesterday_IsRefusedWithoutStoringTheGuest()
    {
        // Arrange
        ServiceWorld world = new();
        world.AddRoom();
        CreatePublicBookingRequest request = new(world.Clock.Today.AddDays(-1), world.Clock.Today.AddDays(2), RoomSize.Double, 1, ServiceWorld.ValidGuestFields());

        // Act
        Result<BookingDetailsDto> result = await world.BookingService().CreatePublicAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.StartDateInPast, result.FirstError?.Code);
        AssertNothingStored(world);
    }

    /// <summary>Negative: a field of the guest that breaks its rule is refused with the code of the field, and nothing is stored.</summary>
    [Theory]
    [InlineData("Anna1", "Jensen", "anna@example.com", "+4512345678", "Denmark", ErrorCodes.Guest.FirstNameInvalid)]
    [InlineData("Anna", "<b>", "anna@example.com", "+4512345678", "Denmark", ErrorCodes.Guest.LastNameInvalid)]
    [InlineData("Anna", "Jensen", "Anna <anna@example.com>", "+4512345678", "Denmark", ErrorCodes.Guest.EmailInvalid)]
    [InlineData("Anna", "Jensen", "anna@example.com", "call me", "Denmark", ErrorCodes.Guest.PhoneNumberInvalid)]
    [InlineData("Anna", "Jensen", "anna@example.com", "+4512345678", "Testland", ErrorCodes.Guest.CountryInvalid)]
    public async Task CreatePublicAsync_GuestFieldBreaksItsRule_IsRefusedWithItsCode(
        string firstName, string lastName, string email, string phoneNumber, string country, string code)
    {
        // Arrange
        ServiceWorld world = new();
        world.AddRoom();
        GuestFields fields = new(firstName, lastName, email, phoneNumber, country, null);
        CreatePublicBookingRequest request = new(world.Clock.Today.AddDays(1), world.Clock.Today.AddDays(3), RoomSize.Double, 1, fields);

        // Act
        Result<BookingDetailsDto> result = await world.BookingService().CreatePublicAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(new[] { code }, result.Errors.Select(error => error.Code));
        AssertNothingStored(world);
    }

    /// <summary>Positive: the stored guest holds the normal form of what was typed: trimmed, letters composed.</summary>
    [Fact]
    public async Task CreatePublicAsync_FieldsWithWhiteSpace_StoresTheNormalForm()
    {
        // Arrange
        ServiceWorld world = new();
        world.AddRoom();
        GuestFields fields = new(" José ", " Jensen ", " anna@example.com ", " +45 12 34 56 78 ", " Denmark ", " ");
        CreatePublicBookingRequest request = new(world.Clock.Today.AddDays(1), world.Clock.Today.AddDays(3), RoomSize.Double, 1, fields);

        // Act
        Result<BookingDetailsDto> result = await world.BookingService().CreatePublicAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Guest guest = Assert.Single(world.Guests.Items);
        Assert.Equal(
            ("José", "Jensen", "anna@example.com", "+45 12 34 56 78", "Denmark", (string?)null),
            (guest.FirstName, guest.LastName, guest.Email, guest.PhoneNumber, guest.Country, guest.PassportNumber));
    }

    #endregion

    #region The booking and the move by staff

    /// <summary>Positive: staff may book 31 nights and more within the five years (BR-C-07 holds for the public site only).</summary>
    [Fact]
    public async Task CreateAsync_StaffBookThirtyOneNights_IsAccepted()
    {
        // Arrange
        ServiceWorld world = new();
        Room room = world.AddRoom();
        CreateBookingRequest request = new(world.Clock.Today, world.Clock.Today.AddDays(31), room.Id, null, ServiceWorld.ValidGuestFields());

        // Act
        Result<BookingDetailsDto> result = await world.BookingService(Allow(Permissions.CreateBooking)).CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
    }

    /// <summary>Boundary: staff may book up to exactly five years ahead; a day later is refused and the new guest is not stored.</summary>
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public async Task CreateAsync_Departure_IsAtMostFiveYearsAheadForStaff(int daysBeyond, bool accepted)
    {
        // Arrange
        ServiceWorld world = new();
        Room room = world.AddRoom();
        DateOnly departure = LastDay.AddDays(daysBeyond);
        CreateBookingRequest request = new(departure.AddDays(-60), departure, room.Id, null, ServiceWorld.ValidGuestFields());

        // Act
        Result<BookingDetailsDto> result = await world.BookingService(Allow(Permissions.CreateBooking)).CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        if (accepted)
        {
            Assert.True(result.IsSuccess);
        }
        else
        {
            Assert.Equal(ErrorCodes.Booking.EndDateTooFarAhead, result.FirstError?.Code);
            AssertNothingStored(world);
        }
    }

    /// <summary>Negative: an arrival yesterday with a new guest is refused before the guest is stored.</summary>
    [Fact]
    public async Task CreateAsync_ArrivalYesterdayWithNewGuest_StoresNoGuest()
    {
        // Arrange
        ServiceWorld world = new();
        Room room = world.AddRoom();
        CreateBookingRequest request = new(world.Clock.Today.AddDays(-1), world.Clock.Today.AddDays(2), room.Id, null, ServiceWorld.ValidGuestFields());

        // Act
        Result<BookingDetailsDto> result = await world.BookingService(Allow(Permissions.CreateBooking)).CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.StartDateInPast, result.FirstError?.Code);
        AssertNothingStored(world);
    }

    /// <summary>
    /// Boundary: the day turns after the stay is checked and the new guest is saved; the booking is made against the
    /// day that was checked, so no guest is left stored without its booking.
    /// </summary>
    [Fact]
    public async Task CreateAsync_DayTurnsAfterTheCheck_BooksAgainstTheCheckedDay()
    {
        // Arrange
        ServiceWorld world = new();
        Room room = world.AddRoom();
        DateOnly today = world.Clock.Today;
        CreateBookingRequest request = new(today, today.AddDays(2), room.Id, null, ServiceWorld.ValidGuestFields());
        world.Clock.NextDay = today.AddDays(1);

        // Act
        Result<BookingDetailsDto> result = await world.BookingService(Allow(Permissions.CreateBooking)).CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess, result.FirstError?.Code);
        Assert.Equal(today, Assert.Single(world.Bookings.Items).StartDate);
        Assert.Single(world.Guests.Items);
    }

    /// <summary>Boundary: staff may move a booking to end exactly five years ahead, and 31 nights; a day later is refused and changes nothing.</summary>
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public async Task RescheduleAsync_NewDeparture_IsAtMostFiveYearsAhead(int daysBeyond, bool accepted)
    {
        // Arrange
        ServiceWorld world = new();
        Room room = world.AddRoom();
        Booking booking = world.AddBooking(room.Id, world.AddGuest().Id, startOffsetDays: 2, nights: 2);
        (DateOnly start, DateOnly end) = (booking.StartDate, booking.EndDate);
        DateOnly departure = LastDay.AddDays(daysBeyond);
        RescheduleBookingRequest request = new(booking.Id, departure.AddDays(-31), departure, room.Id);

        // Act
        Result<BookingDetailsDto> result = await world.BookingService(Allow(Permissions.EditOtherBookings)).RescheduleAsync(request, TestContext.Current.CancellationToken);

        // Assert
        if (accepted)
        {
            Assert.True(result.IsSuccess);
            Assert.Equal(departure, booking.EndDate);
        }
        else
        {
            Assert.Equal(ErrorCodes.Booking.EndDateTooFarAhead, result.FirstError?.Code);
            Assert.Equal((start, end), (booking.StartDate, booking.EndDate));
        }
    }

    #endregion

    #region The move by a guest

    /// <summary>Boundary: a guest moves a booking to a stay of 30 nights; 31 are refused and change nothing (BR-C-07).</summary>
    [Theory]
    [InlineData(30, true)]
    [InlineData(31, false)]
    public async Task GuestRescheduleAsync_Nights_AreAtMostThirty(int nights, bool accepted)
    {
        // Arrange
        ServiceWorld world = new();
        Room room = world.AddRoom();
        Booking own = world.AddBooking(room.Id, world.AddGuest().Id, startOffsetDays: 3, nights: 2);
        world.GuestCall.BookingId = own.Id;
        DateOnly start = own.StartDate;
        GuestRescheduleRequest request = new(world.Clock.Today.AddDays(10), world.Clock.Today.AddDays(10 + nights));

        // Act
        Result<BookingReceiptDto> result = await world.GuestBookingService().RescheduleAsync(request, TestContext.Current.CancellationToken);

        // Assert
        if (accepted)
        {
            Assert.True(result.IsSuccess);
        }
        else
        {
            Assert.Equal(ErrorCodes.Booking.StayTooLong, result.FirstError?.Code);
            Assert.Equal(start, own.StartDate);
        }
    }

    /// <summary>Boundary: a guest moves a booking to end exactly five years ahead; a day later is refused.</summary>
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public async Task GuestRescheduleAsync_Departure_IsAtMostFiveYearsAhead(int daysBeyond, bool accepted)
    {
        // Arrange
        ServiceWorld world = new();
        Room room = world.AddRoom();
        Booking own = world.AddBooking(room.Id, world.AddGuest().Id, startOffsetDays: 3, nights: 2);
        world.GuestCall.BookingId = own.Id;
        DateOnly departure = LastDay.AddDays(daysBeyond);
        GuestRescheduleRequest request = new(departure.AddDays(-5), departure);

        // Act
        Result<BookingReceiptDto> result = await world.GuestBookingService().RescheduleAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(accepted ? null : ErrorCodes.Booking.EndDateTooFarAhead, result.FirstError?.Code);
    }

    #endregion

    #region The free rooms

    /// <summary>Boundary: the free rooms are found up to exactly five years ahead; a period that ends a day later is refused.</summary>
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public async Task GetAvailableRoomsAsync_Departure_IsAtMostFiveYearsAhead(int daysBeyond, bool accepted)
    {
        // Arrange
        ServiceWorld world = new();
        world.AddRoom();
        DateOnly departure = LastDay.AddDays(daysBeyond);

        // Act
        Result<IReadOnlyList<AvailableRoomDto>> result = await world.BookingService()
            .GetAvailableRoomsAsync(departure.AddDays(-2), departure, null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(accepted ? null : ErrorCodes.Booking.EndDateTooFarAhead, result.FirstError?.Code);
    }

    /// <summary>Negative: no free rooms are looked up for an arrival yesterday.</summary>
    [Fact]
    public async Task GetAvailableRoomsAsync_ArrivalYesterday_IsRefused()
    {
        // Arrange
        ServiceWorld world = new();
        world.AddRoom();

        // Act
        Result<IReadOnlyList<AvailableRoomDto>> result = await world.BookingService()
            .GetAvailableRoomsAsync(world.Clock.Today.AddDays(-1), world.Clock.Today.AddDays(1), null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.StartDateInPast, result.FirstError?.Code);
    }

    #endregion

    private static void AssertNothingStored(ServiceWorld world)
    {
        Assert.Empty(world.Bookings.Items);
        Assert.Empty(world.Guests.Items);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    private static IStaffAuthorizationService Allow(string permission)
    {
        IStaffAuthorizationService authorization = Substitute.For<IStaffAuthorizationService>();
        authorization.HasPermissionAsync(permission, Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        return authorization;
    }
}
