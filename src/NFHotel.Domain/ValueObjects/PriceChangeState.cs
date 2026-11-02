namespace NFHotel.Domain.ValueObjects;

/// <summary>The state of a price change (MIL-055). A change is never deleted; a revert only changes its state.</summary>
public enum PriceChangeState
{
    /// <summary>A Manager approved the change; it counts for its whole period.</summary>
    Approved = 1,

    /// <summary>A Manager reverted the change: it was withdrawn, or it ended before its last day.</summary>
    Reverted = 2,
}
