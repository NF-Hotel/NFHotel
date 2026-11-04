namespace NFHotel.Domain.ValueObjects;

/// <summary>Where a trade of rooms or of shifts between members of staff is (MIL-060).</summary>
public enum TradeState
{
    /// <summary>Offered; waiting for the colleague to take, accept or decline it.</summary>
    Open = 1,

    /// <summary>The colleague agreed; waiting for a Manager.</summary>
    AwaitingApproval = 2,

    /// <summary>Carried out: the rooms or the shifts changed hands.</summary>
    Done = 3,

    /// <summary>The colleague said no.</summary>
    Declined = 4,

    /// <summary>A Manager said no.</summary>
    Rejected = 5,

    /// <summary>The member who offered it took it back.</summary>
    Withdrawn = 6,

    /// <summary>Called off by the system: a room or a shift of the trade changed hands in another way meanwhile.</summary>
    Cancelled = 7,
}
