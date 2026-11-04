namespace NFHotel.Domain.Errors;

/// <summary>Error codes of the shifts and of the trades of shifts between members of staff (MIL-060).</summary>
public static class ShiftErrors
{
    /// <summary>The shift does not exist.</summary>
    public const string NotFound = "shift.not_found";

    /// <summary>The shift ends before or when it starts.</summary>
    public const string EndBeforeStart = "shift.end_before_start";

    /// <summary>The shift is longer than <see cref="Entities.Shift.MaxLength"/>.</summary>
    public const string TooLong = "shift.too_long";

    /// <summary>The role of the shift is not one of <see cref="ValueObjects.StaffRole"/>.</summary>
    public const string RoleInvalid = "shift.role_invalid";

    /// <summary>The member of staff is not active, or has another role than the shift.</summary>
    public const string AssigneeInvalid = "shift.assignee_invalid";

    /// <summary>The period asked for ends before it starts, or is longer than 62 days.</summary>
    public const string PeriodInvalid = "shift.period_invalid";

    /// <summary>The member of staff has another shift at the same time.</summary>
    public const string Overlap = "shift.overlap";

    /// <summary>The trade does not exist.</summary>
    public const string TradeNotFound = "shift_trade.not_found";

    /// <summary>The caller is not the member of staff who may take that step.</summary>
    public const string TradeNotYours = "shift_trade.not_yours";

    /// <summary>The trade is not in a state that allows the step.</summary>
    public const string TradeNotOpen = "shift_trade.not_open";

    /// <summary>The shift starts within <see cref="Entities.ShiftTrade.Cutoff"/>, so it can no longer be traded.</summary>
    public const string TradeExpired = "shift_trade.expired";

    /// <summary>The shift already has an open trade.</summary>
    public const string TradeAlreadyOffered = "shift_trade.already_offered";

    /// <summary>The caller's role is not the role of the shift.</summary>
    public const string TradeWrongRole = "shift_trade.wrong_role";

    /// <summary>The caller cannot take the own shift, or swap with the own shift.</summary>
    public const string TradeOwnShift = "shift_trade.own_shift";

    /// <summary>The shift asked for in a swap belongs to nobody, or has another role.</summary>
    public const string TradeSwapInvalid = "shift_trade.swap_invalid";

    /// <summary>A shift of the trade changed hands meanwhile, so the trade was called off.</summary>
    public const string TradeShiftChanged = "shift_trade.shift_changed";
}
