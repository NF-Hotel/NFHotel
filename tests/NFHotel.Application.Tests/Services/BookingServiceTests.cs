using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

using NSubstitute;

namespace NFHotel.Application.Tests.Services;

/// <summary>
/// Unit tests of <c>BookingService</c> with in-memory repositories, a fake unit of work and a fake
/// clock: no database, no sleeping, and "today" always comes from the clock (A-05).
/// </summary>
public sealed class BookingServiceTests
{
    #region Reading a booking

    /// <summary>Positive: an existing booking is described from its read model.</summary>
    [Fact]
    public async Task GetByIdAsync_ExistingBooking_ReturnsItsDetails()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var guest = world.AddGuest();
        var booking = world.AddBooking(room.Id, guest.Id);

        // Act
        var result = await world.BookingService().GetByIdAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(booking.Id, result.Value.BookingId);
    }

    /// <summary>Negative: a booking that does not exist is reported with the not-found code.</summary>
    [Fact]
    public async Task GetByIdAsync_UnknownBooking_ReturnsNotFound()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await world.BookingService().GetByIdAsync(99, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.NotFound, result.FirstError?.Code);
    }

    /// <summary>Boundary: zero and a negative key are programming errors, not a missing booking.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByIdAsync_KeyBelowOne_ThrowsArgumentOutOfRange(int bookingId)
    {
        // Arrange
        var service = new ServiceWorld().BookingService();

        // Act and assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetByIdAsync(bookingId, TestContext.Current.CancellationToken));
    }

    #endregion

    #region Overview

    /// <summary>Positive: by default cancelled bookings are not asked for (BR-13).</summary>
    [Fact]
    public async Task GetOverviewAsync_CancelledNotIncluded_AsksOnlyForBlockingStatuses()
    {
        // Arrange
        var world = new ServiceWorld();
        var query = new BookingOverviewQuery(world.Clock.Today, world.Clock.Today.AddDays(30));
        world.BookingQueries
            .GetOverviewAsync(default, default, default!, default, TestContext.Current.CancellationToken)
            .ReturnsForAnyArgs(Task.FromResult<IReadOnlyList<BookingReadModel>>([]));

        // Act
        var result = await world.BookingService().GetOverviewAsync(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        await world.BookingQueries.Received(1).GetOverviewAsync(
            query.PeriodStart,
            query.PeriodEnd,
            Arg.Is<IReadOnlyCollection<BookingStatus>>(statuses => !statuses.Contains(BookingStatus.Cancelled)),
            null,
            TestContext.Current.CancellationToken);
    }

    /// <summary>Edge: with <c>IncludeCancelled</c> every status is asked for.</summary>
    [Fact]
    public async Task GetOverviewAsync_CancelledIncluded_AsksForEveryStatus()
    {
        // Arrange
        var world = new ServiceWorld();
        var query = new BookingOverviewQuery(world.Clock.Today, world.Clock.Today.AddDays(30), IncludeCancelled: true);
        world.BookingQueries
            .GetOverviewAsync(default, default, default!, default, TestContext.Current.CancellationToken)
            .ReturnsForAnyArgs(Task.FromResult<IReadOnlyList<BookingReadModel>>([]));

        // Act
        await world.BookingService().GetOverviewAsync(query, TestContext.Current.CancellationToken);

        // Assert
        await world.BookingQueries.Received(1).GetOverviewAsync(
            query.PeriodStart,
            query.PeriodEnd,
            Arg.Is<IReadOnlyCollection<BookingStatus>>(statuses => statuses.Contains(BookingStatus.Cancelled)),
            null,
            TestContext.Current.CancellationToken);
    }

    /// <summary>Positive: the list is sorted as asked, with the booking number as tie-breaker.</summary>
    [Fact]
    public async Task GetOverviewAsync_SortedByStartDateDescending_ReturnsLatestStartFirst()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var guest = world.AddGuest();
        var early = world.AddBooking(room.Id, guest.Id, startOffsetDays: 1, nights: 1);
        var late = world.AddBooking(room.Id, guest.Id, startOffsetDays: 10, nights: 1);
        var query = new BookingOverviewQuery(
            world.Clock.Today,
            world.Clock.Today.AddDays(30),
            Direction: SortDirection.Descending);
        world.BookingQueries
            .GetOverviewAsync(default, default, default!, default, TestContext.Current.CancellationToken)
            .ReturnsForAnyArgs(Task.FromResult<IReadOnlyList<BookingReadModel>>(
                [world.ReadModelFor(early), world.ReadModelFor(late)]));

        // Act
        var result = await world.BookingService().GetOverviewAsync(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([late.Id, early.Id], result.Value.Select(item => item.BookingId).ToArray());
    }

    /// <summary>Negative: a missing query is a programming error.</summary>
    [Fact]
    public async Task GetOverviewAsync_NullQuery_ThrowsArgumentNull()
    {
        // Arrange
        var service = new ServiceWorld().BookingService();

        // Act and assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.GetOverviewAsync(null!, TestContext.Current.CancellationToken));
    }

    #endregion

    #region Available rooms

    /// <summary>Positive: a bookable room without bookings in the period is offered.</summary>
    [Fact]
    public async Task GetAvailableRoomsAsync_RoomWithoutBookings_OffersTheRoom()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        ArrangeRoomsAndOccupancy(world, [room], []);

        // Act
        var result = await world.BookingService().GetAvailableRoomsAsync(
            world.Clock.Today, world.Clock.Today.AddDays(3), null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([room.Id], result.Value.Select(available => available.RoomId).ToArray());
    }

    /// <summary>Negative: a room that is occupied during the period is not offered (BR-39).</summary>
    [Fact]
    public async Task GetAvailableRoomsAsync_RoomOccupiedInThePeriod_DoesNotOfferTheRoom()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var occupancy = new RoomOccupancyDto(1, room.Id, world.Clock.Today, world.Clock.Today.AddDays(5), BookingStatus.Confirmed);
        ArrangeRoomsAndOccupancy(world, [room], [occupancy]);

        // Act
        var result = await world.BookingService().GetAvailableRoomsAsync(
            world.Clock.Today.AddDays(1), world.Clock.Today.AddDays(3), null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result.Value);
    }

    /// <summary>Boundary: a departure on the arrival day is no overlap, the interval is half-open (BR-39).</summary>
    [Fact]
    public async Task GetAvailableRoomsAsync_OtherGuestDepartsOnArrivalDay_OffersTheRoom()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var occupancy = new RoomOccupancyDto(1, room.Id, world.Clock.Today, world.Clock.Today.AddDays(3), BookingStatus.Confirmed);
        ArrangeRoomsAndOccupancy(world, [room], [occupancy]);

        // Act
        var result = await world.BookingService().GetAvailableRoomsAsync(
            world.Clock.Today.AddDays(3), world.Clock.Today.AddDays(6), null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(result.Value);
    }

    /// <summary>Edge: the booking being moved does not block its own room.</summary>
    [Fact]
    public async Task GetAvailableRoomsAsync_OccupiedByTheExcludedBooking_OffersTheRoom()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var occupancy = new RoomOccupancyDto(7, room.Id, world.Clock.Today, world.Clock.Today.AddDays(5), BookingStatus.Confirmed);
        ArrangeRoomsAndOccupancy(world, [room], [occupancy]);

        // Act
        var result = await world.BookingService().GetAvailableRoomsAsync(
            world.Clock.Today, world.Clock.Today.AddDays(5), 7, TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(result.Value);
    }

    /// <summary>Negative: a period that ends on or before its start is rejected before any lookup.</summary>
    [Fact]
    public async Task GetAvailableRoomsAsync_EndNotAfterStart_ReturnsEndNotAfterStart()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await world.BookingService().GetAvailableRoomsAsync(
            world.Clock.Today, world.Clock.Today, null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.EndNotAfterStart, result.FirstError?.Code);
    }

    #endregion

    #region Creating a booking

    /// <summary>Positive: an existing guest and a free room give a pending booking, saved once.</summary>
    [Fact]
    public async Task CreateAsync_ExistingGuestAndFreeRoom_CreatesAPendingBookingAndSaves()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var guest = world.AddGuest();
        var request = new CreateBookingRequest(world.Clock.Today, world.Clock.Today.AddDays(2), room.Id, guest.Id, null);

        // Act
        var result = await world.BookingService().CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Pending, world.Bookings.Items.Single().Status);
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>Positive: a new guest is saved first, then the booking refers to the new key.</summary>
    [Fact]
    public async Task CreateAsync_NewGuest_SavesTheGuestAndTheBooking()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var request = new CreateBookingRequest(world.Clock.Today, world.Clock.Today.AddDays(2), room.Id, null, ServiceWorld.ValidGuestFields());

        // Act
        var result = await world.BookingService().CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(world.Guests.Items.Single().Id, world.Bookings.Items.Single().GuestId);
        Assert.Equal(2, world.UnitOfWork.SaveCount);
    }

    /// <summary>Equivalence: neither of the two guest paths, and both of them, are the same rule error (BR-47).</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task CreateAsync_NeitherOrBothGuestPaths_ReturnsGuestSelectionInvalid(bool withGuestId, bool withNewGuest)
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var guest = world.AddGuest();
        var request = new CreateBookingRequest(
            world.Clock.Today,
            world.Clock.Today.AddDays(2),
            room.Id,
            withGuestId ? guest.Id : null,
            withNewGuest ? ServiceWorld.ValidGuestFields() : null);

        // Act
        var result = await world.BookingService().CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.GuestSelectionInvalid, result.FirstError?.Code);
        Assert.Empty(world.Bookings.Items);
    }

    /// <summary>Boundary: a one-night stay is the shortest allowed period.</summary>
    [Fact]
    public async Task CreateAsync_OneNightStay_IsAccepted()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var guest = world.AddGuest();
        var request = new CreateBookingRequest(world.Clock.Today, world.Clock.Today.AddDays(1), room.Id, guest.Id, null);

        // Act
        var result = await world.BookingService().CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
    }

    /// <summary>Negative boundary: a departure on the arrival day is no stay at all (BR-103).</summary>
    [Fact]
    public async Task CreateAsync_DepartureOnArrivalDay_ReturnsEndNotAfterStart()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var guest = world.AddGuest();
        var request = new CreateBookingRequest(world.Clock.Today, world.Clock.Today, room.Id, guest.Id, null);

        // Act
        var result = await world.BookingService().CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.EndNotAfterStart, result.FirstError?.Code);
    }

    /// <summary>Boundary: yesterday is too early, today is allowed (BR-44, BR-104).</summary>
    [Fact]
    public async Task CreateAsync_StartYesterday_ReturnsStartDateInPast()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var guest = world.AddGuest();
        var request = new CreateBookingRequest(world.Clock.Today.AddDays(-1), world.Clock.Today.AddDays(2), room.Id, guest.Id, null);

        // Act
        var result = await world.BookingService().CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.StartDateInPast, result.FirstError?.Code);
    }

    /// <summary>Negative: no room chosen.</summary>
    [Fact]
    public async Task CreateAsync_NoRoomChosen_ReturnsRoomRequired()
    {
        // Arrange
        var world = new ServiceWorld();
        var guest = world.AddGuest();
        var request = new CreateBookingRequest(world.Clock.Today, world.Clock.Today.AddDays(2), 0, guest.Id, null);

        // Act
        var result = await world.BookingService().CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.RoomRequired, result.FirstError?.Code);
    }

    /// <summary>Negative: the chosen room does not exist.</summary>
    [Fact]
    public async Task CreateAsync_UnknownRoom_ReturnsRoomNotFound()
    {
        // Arrange
        var world = new ServiceWorld();
        var guest = world.AddGuest();
        var request = new CreateBookingRequest(world.Clock.Today, world.Clock.Today.AddDays(2), 99, guest.Id, null);

        // Act
        var result = await world.BookingService().CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Room.NotFound, result.FirstError?.Code);
    }

    /// <summary>Negative: a room that is out of service cannot be let (BR-110).</summary>
    [Fact]
    public async Task CreateAsync_RoomOutOfService_ReturnsRoomNotBookable()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        room.TakeOutOfService();
        var guest = world.AddGuest();
        var request = new CreateBookingRequest(world.Clock.Today, world.Clock.Today.AddDays(2), room.Id, guest.Id, null);

        // Act
        var result = await world.BookingService().CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.RoomNotBookable, result.FirstError?.Code);
    }

    /// <summary>Negative: an overlap with a confirmed booking of the same room is refused (BR-19).</summary>
    [Fact]
    public async Task CreateAsync_PeriodOverlapsAnotherBooking_ReturnsRoomNotAvailable()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var guest = world.AddGuest();
        world.AddBooking(room.Id, guest.Id, startOffsetDays: 0, nights: 5);
        var request = new CreateBookingRequest(world.Clock.Today.AddDays(2), world.Clock.Today.AddDays(4), room.Id, guest.Id, null);

        // Act
        var result = await world.BookingService().CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.RoomNotAvailable, result.FirstError?.Code);
    }

    /// <summary>Boundary: arriving on the day the other guest leaves is allowed (BR-39).</summary>
    [Fact]
    public async Task CreateAsync_ArrivalOnTheDayAnotherBookingEnds_IsAccepted()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var guest = world.AddGuest();
        world.AddBooking(room.Id, guest.Id, startOffsetDays: 0, nights: 3);
        var request = new CreateBookingRequest(world.Clock.Today.AddDays(3), world.Clock.Today.AddDays(5), room.Id, guest.Id, null);

        // Act
        var result = await world.BookingService().CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
    }

    /// <summary>Equivalence: a cancelled booking does not block the room, so the same period can be booked again.</summary>
    [Fact]
    public async Task CreateAsync_PeriodOfACancelledBooking_IsAccepted()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var guest = world.AddGuest();
        world.AddBooking(room.Id, guest.Id, startOffsetDays: 0, nights: 3).Cancel();
        var request = new CreateBookingRequest(world.Clock.Today, world.Clock.Today.AddDays(3), room.Id, guest.Id, null);

        // Act
        var result = await world.BookingService().CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
    }

    /// <summary>Negative: a guest key that does not exist.</summary>
    [Fact]
    public async Task CreateAsync_UnknownGuestId_ReturnsGuestNotFound()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var request = new CreateBookingRequest(world.Clock.Today, world.Clock.Today.AddDays(2), room.Id, 99, null);

        // Act
        var result = await world.BookingService().CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Guest.NotFound, result.FirstError?.Code);
    }

    /// <summary>Error handling: invalid fields of a new guest are reported together and nothing is saved.</summary>
    [Fact]
    public async Task CreateAsync_NewGuestWithEmptyFields_ReturnsFieldErrorsAndSavesNothing()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var fields = ServiceWorld.ValidGuestFields() with { FirstName = " ", LastName = " " };
        var request = new CreateBookingRequest(world.Clock.Today, world.Clock.Today.AddDays(2), room.Id, null, fields);

        // Act
        var result = await world.BookingService().CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(ErrorCodes.Guest.FirstNameRequired, result.Errors.Select(error => error.Code));
        Assert.Contains(ErrorCodes.Guest.LastNameRequired, result.Errors.Select(error => error.Code));
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    /// <summary>Error handling: the database exclusion constraint wins a race and gives the same error code.</summary>
    [Fact]
    public async Task CreateAsync_OverlapDetectedWhileSaving_ReturnsRoomNotAvailable()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var guest = world.AddGuest();
        world.UnitOfWork.FailWith = new BookingOverlapConflictException("another request booked the room first");
        var request = new CreateBookingRequest(world.Clock.Today, world.Clock.Today.AddDays(2), room.Id, guest.Id, null);

        // Act
        var result = await world.BookingService().CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.RoomNotAvailable, result.FirstError?.Code);
    }

    /// <summary>Error handling: a concurrency token mismatch on save is reported, not thrown.</summary>
    [Fact]
    public async Task CreateAsync_ConcurrencyConflictWhileSaving_ReturnsConcurrencyConflict()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var guest = world.AddGuest();
        world.UnitOfWork.FailWith = new ConcurrencyConflictException("row changed");
        var request = new CreateBookingRequest(world.Clock.Today, world.Clock.Today.AddDays(2), room.Id, guest.Id, null);

        // Act
        var result = await world.BookingService().CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.ConcurrencyConflict, result.FirstError?.Code);
    }

    /// <summary>Negative: a missing request is a programming error.</summary>
    [Fact]
    public async Task CreateAsync_NullRequest_ThrowsArgumentNull()
    {
        // Arrange
        var service = new ServiceWorld().BookingService();

        // Act and assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.CreateAsync(null!, TestContext.Current.CancellationToken));
    }

    #endregion

    #region State transitions

    /// <summary>State transition: a pending booking is confirmed and saved.</summary>
    [Fact]
    public async Task ConfirmAsync_PendingBooking_ConfirmsAndSaves()
    {
        // Arrange
        var world = new ServiceWorld();
        var booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id);

        // Act
        var result = await world.BookingService().ConfirmAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>State transition: a cancelled booking cannot be confirmed, and nothing is saved.</summary>
    [Fact]
    public async Task ConfirmAsync_CancelledBooking_ReturnsConfirmNotAllowed()
    {
        // Arrange
        var world = new ServiceWorld();
        var booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id);
        booking.Cancel();

        // Act
        var result = await world.BookingService().ConfirmAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.ConfirmNotAllowed, result.FirstError?.Code);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: an unknown booking cannot be confirmed.</summary>
    [Fact]
    public async Task ConfirmAsync_UnknownBooking_ReturnsNotFound()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await world.BookingService().ConfirmAsync(99, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.NotFound, result.FirstError?.Code);
    }

    /// <summary>State transition: a confirmed booking that starts today is checked in at the clock's time.</summary>
    [Fact]
    public async Task CheckInAsync_ConfirmedBookingStartingToday_ChecksInAtTheClockTime()
    {
        // Arrange
        var world = new ServiceWorld();
        var booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id);
        booking.Confirm();

        // Act
        var result = await world.BookingService().CheckInAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.CheckedIn, booking.Status);
        Assert.Equal(world.Clock.UtcNow, booking.CheckInTime);
    }

    /// <summary>Boundary: the day before the arrival date is too early to check in.</summary>
    [Fact]
    public async Task CheckInAsync_BookingStartingTomorrow_ReturnsCheckInNotAllowed()
    {
        // Arrange
        var world = new ServiceWorld();
        var booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id, startOffsetDays: 1);
        booking.Confirm();

        // Act
        var result = await world.BookingService().CheckInAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.CheckInNotAllowed, result.FirstError?.Code);
    }

    /// <summary>State transition: checking out frees the booking and marks the room for departure cleaning.</summary>
    [Fact]
    public async Task CheckOutAsync_CheckedInBooking_ChecksOutAndMarksTheRoomForDepartureCleaning()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var booking = world.AddBooking(room.Id, world.AddGuest().Id);
        booking.Confirm();
        booking.CheckIn(world.Clock.UtcNow, world.Clock.Today);
        world.Clock.TimeOfDay = TimeSpan.FromHours(18);

        // Act
        var result = await world.BookingService().CheckOutAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.CheckedOut, booking.Status);
        Assert.Equal(HousekeepingStatus.DepartureCleaningDue, room.HousekeepingStatus);
    }

    /// <summary>State transition: only a checked-in booking can be checked out.</summary>
    [Fact]
    public async Task CheckOutAsync_PendingBooking_ReturnsCheckOutNotAllowed()
    {
        // Arrange
        var world = new ServiceWorld();
        var booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id);

        // Act
        var result = await world.BookingService().CheckOutAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.CheckOutNotAllowed, result.FirstError?.Code);
    }

    /// <summary>Negative: an unknown booking cannot be checked out.</summary>
    [Fact]
    public async Task CheckOutAsync_UnknownBooking_ReturnsNotFound()
    {
        // Arrange
        var world = new ServiceWorld();

        // Act
        var result = await world.BookingService().CheckOutAsync(99, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.NotFound, result.FirstError?.Code);
    }

    /// <summary>State transition: a pending booking is cancelled, not deleted (BR-13).</summary>
    [Fact]
    public async Task CancelAsync_PendingBooking_CancelsAndKeepsTheBooking()
    {
        // Arrange
        var world = new ServiceWorld();
        var booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id);

        // Act
        var result = await world.BookingService().CancelAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Single(world.Bookings.Items);
    }

    /// <summary>State transition: a booking that is already checked out cannot be cancelled.</summary>
    [Fact]
    public async Task CancelAsync_CheckedOutBooking_ReturnsCancelNotAllowed()
    {
        // Arrange
        var world = new ServiceWorld();
        var booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id);
        booking.Confirm();
        booking.CheckIn(world.Clock.UtcNow, world.Clock.Today);
        booking.CheckOut(world.Clock.UtcNow.AddHours(6), world.Clock.Today, world.Clock.Today);

        // Act
        var result = await world.BookingService().CancelAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.CancelNotAllowed, result.FirstError?.Code);
    }

    /// <summary>Error handling: a concurrency conflict while saving a transition is reported.</summary>
    [Fact]
    public async Task CancelAsync_ConcurrencyConflictWhileSaving_ReturnsConcurrencyConflict()
    {
        // Arrange
        var world = new ServiceWorld();
        var booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id);
        world.UnitOfWork.FailWith = new ConcurrencyConflictException("row changed");

        // Act
        var result = await world.BookingService().CancelAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.ConcurrencyConflict, result.FirstError?.Code);
    }

    #endregion

    #region Rescheduling

    /// <summary>Positive: a booking moves to a free period of the same room.</summary>
    [Fact]
    public async Task RescheduleAsync_FreePeriod_MovesTheBooking()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var booking = world.AddBooking(room.Id, world.AddGuest().Id, startOffsetDays: 0, nights: 2);
        var request = new RescheduleBookingRequest(booking.Id, world.Clock.Today.AddDays(10), world.Clock.Today.AddDays(12), room.Id);

        // Act
        var result = await world.BookingService().RescheduleAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(world.Clock.Today.AddDays(10), booking.StartDate);
    }

    /// <summary>Edge: the booking does not block its own old period, so a small shift is accepted.</summary>
    [Fact]
    public async Task RescheduleAsync_PeriodOverlappingItself_IsAccepted()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var booking = world.AddBooking(room.Id, world.AddGuest().Id, startOffsetDays: 0, nights: 4);
        var request = new RescheduleBookingRequest(booking.Id, world.Clock.Today.AddDays(1), world.Clock.Today.AddDays(5), room.Id);

        // Act
        var result = await world.BookingService().RescheduleAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
    }

    /// <summary>Negative: a move into the period of another booking is refused (BR-19).</summary>
    [Fact]
    public async Task RescheduleAsync_PeriodOverlapsAnotherBooking_ReturnsRoomNotAvailable()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var guest = world.AddGuest();
        var moving = world.AddBooking(room.Id, guest.Id, startOffsetDays: 0, nights: 2);
        world.AddBooking(room.Id, guest.Id, startOffsetDays: 10, nights: 3);
        var request = new RescheduleBookingRequest(moving.Id, world.Clock.Today.AddDays(11), world.Clock.Today.AddDays(13), room.Id);

        // Act
        var result = await world.BookingService().RescheduleAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.RoomNotAvailable, result.FirstError?.Code);
    }

    /// <summary>Negative: an unknown booking cannot be moved.</summary>
    [Fact]
    public async Task RescheduleAsync_UnknownBooking_ReturnsNotFound()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var request = new RescheduleBookingRequest(99, world.Clock.Today, world.Clock.Today.AddDays(2), room.Id);

        // Act
        var result = await world.BookingService().RescheduleAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.NotFound, result.FirstError?.Code);
    }

    /// <summary>State transition: a cancelled booking cannot be moved.</summary>
    [Fact]
    public async Task RescheduleAsync_CancelledBooking_ReturnsRescheduleNotAllowed()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var booking = world.AddBooking(room.Id, world.AddGuest().Id);
        booking.Cancel();
        var request = new RescheduleBookingRequest(booking.Id, world.Clock.Today.AddDays(5), world.Clock.Today.AddDays(7), room.Id);

        // Act
        var result = await world.BookingService().RescheduleAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.RescheduleNotAllowed, result.FirstError?.Code);
    }

    /// <summary>Boundary: a period that ends on its start day is rejected before the booking is read.</summary>
    [Fact]
    public async Task RescheduleAsync_EndOnStartDay_ReturnsEndNotAfterStart()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var booking = world.AddBooking(room.Id, world.AddGuest().Id);
        var request = new RescheduleBookingRequest(booking.Id, world.Clock.Today.AddDays(5), world.Clock.Today.AddDays(5), room.Id);

        // Act
        var result = await world.BookingService().RescheduleAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.EndNotAfterStart, result.FirstError?.Code);
    }

    #endregion

    #region Scenarios from exploratory testing

    /// <summary>Exploratory: a guest who leaves early frees the rest of the stay, and it can be let again (A-01).</summary>
    [Fact]
    public async Task CreateAsync_AfterAnEarlyCheckOut_LetsTheRestOfTheStayAgain()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var guest = world.AddGuest();
        var booking = world.AddBooking(room.Id, guest.Id, startOffsetDays: 0, nights: 5);
        booking.Confirm();
        booking.CheckIn(world.Clock.UtcNow, world.Clock.Today);
        world.Clock.Today = world.Clock.Today.AddDays(1);
        world.Clock.TimeOfDay = TimeSpan.FromHours(9);
        await world.BookingService().CheckOutAsync(booking.Id, TestContext.Current.CancellationToken);
        var request = new CreateBookingRequest(world.Clock.Today, world.Clock.Today.AddDays(2), room.Id, guest.Id, null);

        // Act
        var result = await world.BookingService().CreateAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
    }

    /// <summary>Exploratory, edge: checking out later on the day of arrival is allowed.</summary>
    [Fact]
    public async Task CheckOutAsync_LaterOnTheDayOfCheckIn_ChecksOut()
    {
        // Arrange
        var world = new ServiceWorld();
        var booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id, startOffsetDays: 0, nights: 2);
        booking.CheckIn(world.Clock.UtcNow, world.Clock.Today);
        world.Clock.TimeOfDay = TimeSpan.FromHours(13);

        // Act
        var result = await world.BookingService().CheckOutAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
    }

    /// <summary>Exploratory, state transition: a guest who is already in the hotel cannot be moved to other dates.</summary>
    [Fact]
    public async Task RescheduleAsync_CheckedInBooking_ReturnsRescheduleNotAllowed()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var booking = world.AddBooking(room.Id, world.AddGuest().Id, startOffsetDays: 0, nights: 2);
        booking.CheckIn(world.Clock.UtcNow, world.Clock.Today);
        var request = new RescheduleBookingRequest(booking.Id, world.Clock.Today.AddDays(5), world.Clock.Today.AddDays(7), room.Id);

        // Act
        var result = await world.BookingService().RescheduleAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.RescheduleNotAllowed, result.FirstError?.Code);
    }

    /// <summary>Exploratory, state transition: cancelling twice is an error the second time, not a silent success.</summary>
    [Fact]
    public async Task CancelAsync_AlreadyCancelledBooking_ReturnsCancelNotAllowed()
    {
        // Arrange
        var world = new ServiceWorld();
        var booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id);
        await world.BookingService().CancelAsync(booking.Id, TestContext.Current.CancellationToken);

        // Act
        var result = await world.BookingService().CancelAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.CancelNotAllowed, result.FirstError?.Code);
    }

    /// <summary>Exploratory, boundary: back-to-back bookings fit, a third that spans both does not.</summary>
    [Fact]
    public async Task CreateAsync_BookingSpanningTwoBackToBackBookings_ReturnsRoomNotAvailable()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var guest = world.AddGuest();
        var service = world.BookingService();
        await service.CreateAsync(new CreateBookingRequest(world.Clock.Today, world.Clock.Today.AddDays(2), room.Id, guest.Id, null), TestContext.Current.CancellationToken);
        await service.CreateAsync(new CreateBookingRequest(world.Clock.Today.AddDays(2), world.Clock.Today.AddDays(4), room.Id, guest.Id, null), TestContext.Current.CancellationToken);

        // Act
        var result = await service.CreateAsync(
            new CreateBookingRequest(world.Clock.Today.AddDays(1), world.Clock.Today.AddDays(3), room.Id, guest.Id, null),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.RoomNotAvailable, result.FirstError?.Code);
    }

    #endregion

    #region Booking guards (MIL-013)

    /// <summary>Negative and boundary: an overview whose end is one day before its start is refused and asks nothing.</summary>
    [Fact]
    public async Task GetOverviewAsync_EndOneDayBeforeStart_ReturnsOverviewPeriodInvalidWithoutQuerying()
    {
        // Arrange
        var world = new ServiceWorld();
        var query = new BookingOverviewQuery(world.Clock.Today, world.Clock.Today.AddDays(-1));

        // Act
        var result = await world.BookingService().GetOverviewAsync(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.OverviewPeriodInvalid, result.FirstError?.Code);
        await world.BookingQueries.DidNotReceiveWithAnyArgs().GetOverviewAsync(
            default, default, default!, default, TestContext.Current.CancellationToken);
    }

    /// <summary>Boundary: an end equal to the start is the view of one day and is allowed.</summary>
    [Fact]
    public async Task GetOverviewAsync_EndEqualsStart_ReturnsTheOneDayView()
    {
        // Arrange
        var world = new ServiceWorld();
        var query = new BookingOverviewQuery(world.Clock.Today, world.Clock.Today);
        world.BookingQueries
            .GetOverviewAsync(default, default, default!, default, TestContext.Current.CancellationToken)
            .ReturnsForAnyArgs(Task.FromResult<IReadOnlyList<BookingReadModel>>([]));

        // Act
        var result = await world.BookingService().GetOverviewAsync(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
    }

    /// <summary>Equivalence: a room out of service and a room in maintenance both refuse the check-in (BR-110).</summary>
    [Theory]
    [InlineData(RoomStatus.OutOfService)]
    [InlineData(RoomStatus.Maintenance)]
    public async Task CheckInAsync_RoomNotBookable_ReturnsRoomNotBookableAndChangesNothing(RoomStatus roomStatus)
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var booking = world.AddBooking(room.Id, world.AddGuest().Id);
        booking.Confirm();
        if (roomStatus == RoomStatus.OutOfService)
        {
            room.TakeOutOfService();
        }
        else
        {
            room.SendToMaintenance();
        }

        // Act
        var result = await world.BookingService().CheckInAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.RoomNotBookable, result.FirstError?.Code);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    /// <summary>Edge: a room that only needs cleaning is still bookable, so the guest may check in (B-04).</summary>
    [Fact]
    public async Task CheckInAsync_RoomNeedsCleaning_ChecksIn()
    {
        // Arrange
        var world = new ServiceWorld();
        var room = world.AddRoom();
        var booking = world.AddBooking(room.Id, world.AddGuest().Id);
        booking.Confirm();
        room.MarkDailyCleaningDue();

        // Act
        var result = await world.BookingService().CheckInAsync(booking.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.CheckedIn, booking.Status);
    }

    #endregion

    #region Helpers

    private static void ArrangeRoomsAndOccupancy(
        ServiceWorld world,
        IReadOnlyList<NFHotel.Domain.Entities.Room> rooms,
        IReadOnlyList<RoomOccupancyDto> occupancies)
    {
        world.RoomQueries.GetByStatusAsync(RoomStatus.Available, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AvailableRoomDto>>(
                rooms.Select(room => new AvailableRoomDto(room.Id, room.RoomNumber, room.Floor, room.Size, room.Capacity)).ToArray()));
        world.BookingQueries.GetOccupancyAsync(
                Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<IReadOnlyCollection<BookingStatus>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(occupancies));
    }

    #endregion
}
