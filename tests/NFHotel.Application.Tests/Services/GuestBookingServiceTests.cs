using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

namespace NFHotel.Application.Tests.Services;

/// <summary>
/// The signed-in guest's own booking (MIL-018, ADR-0049 decisions 3 and 7): read, moved and cancelled without a key in
/// the call, because the booking is the one in the guest token, and answered with the receipt alone.
/// </summary>
public sealed class GuestBookingServiceTests
{
    #region Read

    /// <summary>Positive: a signed-in guest reads the receipt of the booking in the token.</summary>
    [Fact]
    public async Task GetAsync_SignedInGuest_ReadsTheReceiptOfTheBookingInTheToken()
    {
        // Arrange
        ServiceWorld world = new();
        Room room = world.AddRoom();
        Booking own = world.AddBooking(room.Id, world.AddGuest().Id, startOffsetDays: 3, nights: 2);
        world.AddBooking(room.Id, world.AddGuest("Bo", "Berg", "bo@example.com", null).Id, startOffsetDays: 10);
        world.GuestCall.BookingId = own.Id;

        // Act
        Result<BookingReceiptDto> result = await world.GuestBookingService().GetAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(
            new BookingReceiptDto(own.Reference, own.StartDate, own.EndDate, 2, room.Size, BookingStatus.Pending),
            result.Value);
    }

    /// <summary>Security: without a signed-in guest the read gets the one refusal of a guest and reads nothing.</summary>
    [Fact]
    public async Task GetAsync_NoSignedInGuest_GivesTheOneRefusal()
    {
        // Arrange
        ServiceWorld world = new();
        world.AddBooking(world.AddRoom().Id, world.AddGuest().Id);

        // Act
        Result<BookingReceiptDto> result = await world.GuestBookingService().GetAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([ErrorCodes.Api.GuestUnauthorized], result.Errors.Select(error => error.Code));
    }

    /// <summary>
    /// Security (ADR-0047 decision 7, SAR-002 finding 5): a guest token made before the cancellation of its booking reads
    /// only the receipt, with the status Cancelled.
    /// </summary>
    [Fact]
    public async Task GetAsync_CancelledBooking_ReadsTheReceiptWithTheStatusCancelled()
    {
        // Arrange
        ServiceWorld world = new();
        Room room = world.AddRoom();
        Booking own = world.AddBooking(room.Id, world.AddGuest().Id, startOffsetDays: 3, nights: 2);
        own.Cancel();
        world.GuestCall.BookingId = own.Id;

        // Act
        Result<BookingReceiptDto> result = await world.GuestBookingService().GetAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(
            new BookingReceiptDto(own.Reference, own.StartDate, own.EndDate, 2, room.Size, BookingStatus.Cancelled),
            result.Value);
    }

    #endregion Read

    #region Cancel

    /// <summary>Positive: a signed-in guest cancels the booking in the token and gets its receipt with the new status.</summary>
    [Fact]
    public async Task CancelAsync_SignedInGuest_CancelsTheBookingInTheToken()
    {
        // Arrange
        ServiceWorld world = new();
        Booking own = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id, startOffsetDays: 3);
        world.GuestCall.BookingId = own.Id;

        // Act
        Result<BookingReceiptDto> result = await world.GuestBookingService().CancelAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Cancelled, result.Value.Status);
        Assert.Equal(own.Reference, result.Value.Reference);
        Assert.Equal(BookingStatus.Cancelled, own.Status);
    }

    /// <summary>Security: without a signed-in guest nothing is cancelled.</summary>
    [Fact]
    public async Task CancelAsync_NoSignedInGuest_GivesTheOneRefusalAndChangesNothing()
    {
        // Arrange
        ServiceWorld world = new();
        Booking booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id, startOffsetDays: 3);

        // Act
        Result<BookingReceiptDto> result = await world.GuestBookingService().CancelAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.GuestUnauthorized, result.FirstError?.Code);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: a booking that cannot be cancelled keeps the code of the booking.</summary>
    [Fact]
    public async Task CancelAsync_CancelledBooking_KeepsTheCodeOfTheBooking()
    {
        // Arrange
        ServiceWorld world = new();
        Booking own = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id, startOffsetDays: 3);
        own.Cancel();
        world.GuestCall.BookingId = own.Id;

        // Act
        Result<BookingReceiptDto> result = await world.GuestBookingService().CancelAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.NotEqual(ErrorCodes.Api.GuestUnauthorized, result.FirstError?.Code);
    }

    #endregion Cancel

    #region Reschedule

    /// <summary>Positive: a signed-in guest moves the booking in the token to new dates, in the room it has.</summary>
    [Fact]
    public async Task RescheduleAsync_SignedInGuest_MovesTheBookingInItsRoom()
    {
        // Arrange
        ServiceWorld world = new();
        Room room = world.AddRoom();
        world.AddRoom("102");
        Booking own = world.AddBooking(room.Id, world.AddGuest().Id, startOffsetDays: 3, nights: 2);
        world.GuestCall.BookingId = own.Id;
        DateOnly checkIn = world.Clock.Today.AddDays(20);
        DateOnly checkOut = world.Clock.Today.AddDays(23);

        // Act
        Result<BookingReceiptDto> result = await world.GuestBookingService()
            .RescheduleAsync(new GuestRescheduleRequest(checkIn, checkOut), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(new BookingReceiptDto(own.Reference, checkIn, checkOut, 3, room.Size, BookingStatus.Pending), result.Value);
        Assert.Equal(room.Id, own.RoomId);
        Assert.Equal(checkIn, own.StartDate);
    }

    /// <summary>Security: without a signed-in guest nothing is moved.</summary>
    [Fact]
    public async Task RescheduleAsync_NoSignedInGuest_GivesTheOneRefusalAndChangesNothing()
    {
        // Arrange
        ServiceWorld world = new();
        Booking booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id, startOffsetDays: 3);
        DateOnly start = booking.StartDate;

        // Act
        Result<BookingReceiptDto> result = await world.GuestBookingService()
            .RescheduleAsync(new GuestRescheduleRequest(world.Clock.Today.AddDays(20), world.Clock.Today.AddDays(22)), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.GuestUnauthorized, result.FirstError?.Code);
        Assert.Equal(start, booking.StartDate);
    }

    /// <summary>Negative: dates that the rules of a booking refuse keep the code of the booking, and the booking stays.</summary>
    [Fact]
    public async Task RescheduleAsync_ArrivalInThePast_KeepsTheCodeOfTheBooking()
    {
        // Arrange
        ServiceWorld world = new();
        Booking own = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id, startOffsetDays: 3);
        world.GuestCall.BookingId = own.Id;
        DateOnly start = own.StartDate;

        // Act
        Result<BookingReceiptDto> result = await world.GuestBookingService()
            .RescheduleAsync(new GuestRescheduleRequest(world.Clock.Today.AddDays(-2), world.Clock.Today.AddDays(1)), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(start, own.StartDate);
    }

    /// <summary>
    /// Security (ADR-0047 decision 7, SAR-002 finding 5): a guest token made before the cancellation of its booking cannot
    /// move it; the answer is the code of the booking, not the refusal of a guest, and the dates stay.
    /// </summary>
    [Fact]
    public async Task RescheduleAsync_CancelledBooking_KeepsTheCodeOfTheBookingAndTheDates()
    {
        // Arrange
        ServiceWorld world = new();
        Booking own = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id, startOffsetDays: 3, nights: 2);
        own.Cancel();
        world.GuestCall.BookingId = own.Id;
        DateOnly start = own.StartDate;
        DateOnly end = own.EndDate;

        // Act
        Result<BookingReceiptDto> result = await world.GuestBookingService()
            .RescheduleAsync(new GuestRescheduleRequest(world.Clock.Today.AddDays(20), world.Clock.Today.AddDays(22)), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.RescheduleNotAllowed, result.FirstError?.Code);
        Assert.Equal(start, own.StartDate);
        Assert.Equal(end, own.EndDate);
    }

    /// <summary>Negative: the dependencies and the request are required.</summary>
    [Fact]
    public async Task Constructor_MissingDependencyOrRequest_Throws()
    {
        // Arrange
        ServiceWorld world = new();

        // Act and assert: the null-forgiving operator passes the null that the guard must refuse.
        Assert.Throws<ArgumentNullException>(() => new GuestBookingService(null!, world.GuestCall));
        Assert.Throws<ArgumentNullException>(() => new GuestBookingService(world.BookingService(), null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => world.GuestBookingService().RescheduleAsync(null!, TestContext.Current.CancellationToken));
    }

    #endregion Reschedule

    #region The answer

    /// <summary>
    /// ASVS 15.3.1 (ADR-0049 decision 7, MIL-085 criterion 5): the answer to a guest holds the reference, the dates, the
    /// nights, the room size and the status, and nothing else: no key, no guest, no room number.
    /// </summary>
    [Fact]
    public void BookingReceiptDto_Properties_AreTheFieldsOfDecision7()
    {
        // Act
        string[] names = typeof(BookingReceiptDto).GetProperties().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();

        // Assert
        Assert.Equal(["EndDate", "Nights", "Reference", "RoomSize", "StartDate", "Status"], names);
    }

    #endregion The answer
}
