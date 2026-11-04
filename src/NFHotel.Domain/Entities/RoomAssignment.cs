using System.ComponentModel.DataAnnotations;

using NFHotel.Domain.Errors;

namespace NFHotel.Domain.Entities;

/// <summary>
/// The housekeeper who looks after a room, until a Manager or a trade changes it (MIL-060). A room has at most one.
/// </summary>
public sealed class RoomAssignment : Entity<int>
{
    /// <summary>
    /// The fewest rooms a housekeeper keeps. A trade that leaves somebody with fewer than before, and fewer than this,
    /// needs a Manager's approval; a Manager assigns rooms freely.
    /// </summary>
    public const int MinimumRooms = 3;

    private RoomAssignment()
    {
    }

    /// <summary>The room.</summary>
    [Range(1, int.MaxValue, ErrorMessage = RoomTradeErrors.KeyInvalid)]
    public int RoomId { get; private set; }

    /// <summary>The housekeeper.</summary>
    [Range(1, int.MaxValue, ErrorMessage = RoomTradeErrors.KeyInvalid)]
    public int StaffId { get; private set; }

    /// <summary>Assigns a room to a housekeeper.</summary>
    /// <param name="roomId">The room.</param>
    /// <param name="staffId">The housekeeper.</param>
    /// <returns>The assignment.</returns>
    /// <exception cref="DomainException">A key is not valid.</exception>
    public static RoomAssignment Create(int roomId, int staffId)
    {
        var assignment = new RoomAssignment { RoomId = roomId, StaffId = staffId };
        EntityValidation.ThrowIfInvalid(assignment);
        return assignment;
    }

    /// <summary>Gives the room to another housekeeper.</summary>
    /// <param name="staffId">The housekeeper.</param>
    /// <exception cref="DomainException">The key is not valid.</exception>
    public void MoveTo(int staffId)
    {
        if (staffId <= 0)
        {
            throw new DomainException(RoomTradeErrors.KeyInvalid);
        }

        StaffId = staffId;
    }
}
