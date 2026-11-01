using NFHotel.Application.UseCases.Guests;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Tests.Services;

/// <summary>Unit tests of the room and guest mappings to the rows of the screens (MIL-054 task 8).</summary>
public sealed class RoomAndGuestMappingTests
{
    /// <summary>Positive: a room maps to its list row, its actions and its row of free rooms.</summary>
    [Fact]
    public void Room_Maps_ToListItemActionsAndAvailableRoom()
    {
        // Arrange
        var room = Room.Create("204", 2, RoomSize.Suite, 4);

        // Act
        var item = room.ToListItem();
        var actions = room.ToActions();
        var available = room.ToAvailableRoom();

        // Assert
        Assert.Equal(("204", 2, RoomSize.Suite, 4, RoomStatus.Available), (item.RoomNumber, item.Floor, item.Size, item.Capacity, item.Status));
        Assert.Equal(RoomMapping.ToActions(room.Status, room.HousekeepingStatus), actions);
        Assert.Equal(("204", 2, RoomSize.Suite, 4), (available.RoomNumber, available.Floor, available.Size, available.Capacity));
    }

    /// <summary>Equivalence: a guest with a passport number shows only that one is stored; a guest without shows none.</summary>
    [Theory]
    [InlineData("P1234567", true)]
    [InlineData(null, false)]
    public void Guest_ToListItem_ReducesThePassportToAFlag(string? passport, bool expected)
    {
        // Arrange
        var guest = Guest.Create("Anna", "Jensen", "anna@example.com", "+4512345678", "Denmark", passport);

        // Act
        var item = guest.ToListItem();

        // Assert
        Assert.Equal(("Anna", "Jensen", "anna@example.com", "Denmark"), (item.FirstName, item.LastName, item.Email, item.Country));
        Assert.Equal(expected, item.HasPassportNumber);
    }

    /// <summary>Negative: a missing room or guest is refused.</summary>
    [Fact]
    public void Mappings_Null_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => ((Room)null!).ToListItem());
        Assert.Throws<ArgumentNullException>(() => ((Room)null!).ToActions());
        Assert.Throws<ArgumentNullException>(() => ((Room)null!).ToAvailableRoom());
        Assert.Throws<ArgumentNullException>(() => ((Guest)null!).ToListItem());
    }
}
