namespace NFHotel.Domain.ValueObjects;

/// <summary>
/// The bookability of the room — exclusively what prevents letting.
/// </summary>
/// <remarks>
/// Cleaning state belongs in <see cref="HousekeepingStatus"/> (B-04). The ordinal values
/// 0-2 are kept from the old system, so the change costs zero data migration.
/// The old XAML dropdown's <c>Cleaning</c> has moved to <see cref="HousekeepingStatus"/>,
/// <c>Maintanance</c> was a spelling mistake, and <c>Disabled</c> is semantically identical to
/// <see cref="OutOfService"/> (BR-126).
/// </remarks>
public enum RoomStatus
{
    /// <summary>Can be booked. Says nothing about whether the room is clean right now.</summary>
    Available = 0,

    /// <summary>Taken out of service indefinitely (renovation, converted to storage). An administrative decision.</summary>
    OutOfService = 1,

    /// <summary>Temporarily blocked by a fault that makes the room uninhabitable. Expected to return to <see cref="Available"/>.</summary>
    Maintenance = 2
}
