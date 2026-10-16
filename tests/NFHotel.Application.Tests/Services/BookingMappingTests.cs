using NFHotel.Application.UseCases.Bookings;
using NFHotel.Domain.Entities;
using NFHotel.TestSupport;

namespace NFHotel.Application.Tests.Services;

/// <summary>Unit tests of the booking mapping: what the screens may offer for a booking and its room (MIL-013).</summary>
public sealed class BookingMappingTests
{
    #region Check-in flag

    /// <summary>Positive: a confirmed booking of today in a bookable room may be checked in.</summary>
    [Fact]
    public void ToListItem_BookableRoomAndArrivalToday_CanCheckIn()
    {
        // Arrange
        var world = new ServiceWorld();
        var booking = ConfirmedBookingToday(world, out _);

        // Act
        var item = world.ReadModelFor(booking).ToListItem(world.Clock.Today);

        // Assert
        Assert.True(item.Actions.CanCheckIn);
    }

    /// <summary>Negative: the same booking cannot be checked in while its room is out of service.</summary>
    [Fact]
    public void ToListItem_RoomOutOfService_CannotCheckIn()
    {
        // Arrange
        var world = new ServiceWorld();
        var booking = ConfirmedBookingToday(world, out var room);
        room.TakeOutOfService();

        // Act
        var item = world.ReadModelFor(booking).ToListItem(world.Clock.Today);

        // Assert
        Assert.False(item.Actions.CanCheckIn);
    }

    /// <summary>Negative: in the details too, a room in maintenance blocks the check-in flag.</summary>
    [Fact]
    public void ToDetails_RoomInMaintenance_CannotCheckIn()
    {
        // Arrange
        var world = new ServiceWorld();
        var booking = ConfirmedBookingToday(world, out var room);
        room.SendToMaintenance();

        // Act
        var details = world.ReadModelFor(booking).ToDetails(world.Clock.Today);

        // Assert
        Assert.False(details.Actions.CanCheckIn);
    }

    /// <summary>Edge: the other flags do not depend on the room, so cancel and reschedule stay offered.</summary>
    [Fact]
    public void ToListItem_RoomOutOfService_KeepsTheOtherActions()
    {
        // Arrange
        var world = new ServiceWorld();
        var booking = ConfirmedBookingToday(world, out var room);
        room.TakeOutOfService();

        // Act
        var actions = world.ReadModelFor(booking).ToListItem(world.Clock.Today).Actions;

        // Assert
        Assert.True(actions.CanCancel);
        Assert.True(actions.CanReschedule);
    }

    #endregion

    #region Helpers

    private static Booking ConfirmedBookingToday(ServiceWorld world, out Room room)
    {
        room = world.AddRoom();
        var booking = world.AddBooking(room.Id, world.AddGuest().Id);
        booking.Confirm();
        return booking;
    }

    #endregion
}