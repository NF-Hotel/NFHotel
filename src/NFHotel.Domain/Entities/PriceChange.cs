using System.ComponentModel.DataAnnotations;

using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Entities;

/// <summary>
/// A change of the price of one room size for a period (MIL-055): a percentage on top of the base rate, positive for a
/// surcharge and negative for a discount. A change is never deleted. A revert withdraws a change that has not begun, or
/// ends a change in force so that the base rate is back from the day of the revert.
/// </summary>
public sealed class PriceChange : Entity<int>
{
    /// <summary>The longest user name that is stored.</summary>
    public const int MaxTextLength = 100;

    /// <summary>The lowest percentage: a discount of just under 100.</summary>
    public const double MinPercentage = -99.99;

    /// <summary>The highest percentage.</summary>
    public const double MaxPercentage = 1000;

    /// <summary>The number of decimals the percentage is kept with.</summary>
    public const int PercentageDecimals = 2;

    /// <summary>The longest period in days, both ends included.</summary>
    public const int MaxPeriodDays = 366;

    private PriceChange()
    {
    }

    /// <summary>The room size the change is for.</summary>
    [EnumDataType(typeof(RoomSize), ErrorMessage = PriceChangeErrors.SizeInvalid)]
    public RoomSize Size { get; private set; }

    /// <summary>The change in percent: above 0 for a surcharge, below 0 for a discount, never 0.</summary>
    [Range(MinPercentage, MaxPercentage, ErrorMessage = PriceChangeErrors.PercentageInvalid)]
    public decimal Percentage { get; private set; }

    /// <summary>The first day the change is meant for.</summary>
    public DateOnly FirstDay { get; private set; }

    /// <summary>The last day the change is meant for, on or after <see cref="FirstDay"/>.</summary>
    public DateOnly LastDay { get; private set; }

    /// <summary>Whether the change is approved or reverted.</summary>
    public PriceChangeState State { get; private set; } = PriceChangeState.Approved;

    /// <summary>The user name of the staff member who approved the change.</summary>
    [Required(ErrorMessage = PriceChangeErrors.ApprovedByInvalid)]
    [StringLength(MaxTextLength, ErrorMessage = PriceChangeErrors.ApprovedByInvalid)]
    public string ApprovedBy { get; private set; } = string.Empty;

    /// <summary>When the change was approved.</summary>
    public DateTimeOffset ApprovedAt { get; private set; }

    /// <summary>The user name of the staff member who reverted the change; <c>null</c> until it is reverted.</summary>
    public string? RevertedBy { get; private set; }

    /// <summary>When the change was reverted; <c>null</c> until it is reverted.</summary>
    public DateTimeOffset? RevertedAt { get; private set; }

    /// <summary>
    /// The last day a reverted change in force still counted (the day before the revert); <c>null</c> for a change that
    /// is approved and for one that was withdrawn before it began. It is the day before <see cref="FirstDay"/> when the
    /// change is reverted on its first day, so it then counts for no day.
    /// </summary>
    public DateOnly? EndedOn { get; private set; }

    /// <summary>The last day the change counts, or <c>null</c> when it counts for no day (withdrawn).</summary>
    public DateOnly? CountedLastDay =>
        State == PriceChangeState.Approved ? LastDay : EndedOn;

    /// <summary>Creates an approved change.</summary>
    /// <param name="size">The room size.</param>
    /// <param name="percentage">The change in percent; it is rounded to two decimals.</param>
    /// <param name="firstDay">The first day of the period.</param>
    /// <param name="lastDay">The last day of the period.</param>
    /// <param name="approvedBy">Who approves it.</param>
    /// <param name="approvedAt">When it is approved.</param>
    /// <returns>The new change, in the state <see cref="PriceChangeState.Approved"/>.</returns>
    /// <exception cref="DomainException">A field breaks an invariant.</exception>
    public static PriceChange Approve(
        RoomSize size,
        decimal percentage,
        DateOnly firstDay,
        DateOnly lastDay,
        string approvedBy,
        DateTimeOffset approvedAt)
    {
        var candidate = new PriceChange
        {
            Size = size,
            Percentage = Math.Round(percentage, PercentageDecimals, MidpointRounding.AwayFromZero),
            FirstDay = firstDay,
            LastDay = lastDay,
            State = PriceChangeState.Approved,
            ApprovedBy = approvedBy?.Trim() ?? string.Empty,
            ApprovedAt = approvedAt.ToUniversalTime(),
        };

        var codes = new List<string>();
        try
        {
            EntityValidation.ThrowIfInvalid(candidate);
        }
        catch (DomainException exception)
        {
            codes.AddRange(exception.Codes);
        }

        if (candidate.Percentage == 0m && !codes.Contains(PriceChangeErrors.PercentageInvalid))
        {
            codes.Add(PriceChangeErrors.PercentageInvalid);
        }

        // Days are counted with both ends in: one day is a period of 1.
        if (lastDay < firstDay || lastDay.DayNumber - firstDay.DayNumber + 1 > MaxPeriodDays)
        {
            codes.Add(PriceChangeErrors.PeriodInvalid);
        }

        if (codes.Count > 0)
        {
            throw new DomainException(codes.Distinct().ToArray());
        }

        return candidate;
    }

    /// <summary>Tells whether the change counts on a day. It is pure: it reads only the fields of the change.</summary>
    /// <param name="day">The day.</param>
    /// <returns>
    /// <c>true</c> for an approved change inside its period, and for a reverted change inside the days it counted before
    /// the revert; <c>false</c> otherwise.
    /// </returns>
    public bool CountsOn(DateOnly day) => CountsWithin(day, day);

    /// <summary>Tells whether the days the change counts for share a day with a period.</summary>
    /// <param name="first">The first day of the period.</param>
    /// <param name="last">The last day of the period.</param>
    /// <returns><c>true</c> when at least one day is in both.</returns>
    public bool CountsWithin(DateOnly first, DateOnly last)
    {
        var counted = CountedLastDay;

        return counted is not null && FirstDay <= last && counted.Value >= first && first <= last;
    }

    /// <summary>
    /// Reverts the change. A change that has not begun is withdrawn and counts for no day. A change in force ends: it
    /// counted up to and including yesterday, and the base rate is back from <paramref name="today"/>.
    /// </summary>
    /// <param name="revertedBy">Who reverts it.</param>
    /// <param name="revertedAt">When it is reverted.</param>
    /// <param name="today">The day of the revert.</param>
    /// <exception cref="DomainException">
    /// The change is already reverted, its last day is before <paramref name="today"/>, or the name is missing; the change
    /// is not changed.
    /// </exception>
    public void Revert(string revertedBy, DateTimeOffset revertedAt, DateOnly today)
    {
        var name = revertedBy?.Trim() ?? string.Empty;
        if (State == PriceChangeState.Reverted)
        {
            throw new DomainException(PriceChangeErrors.AlreadyReverted);
        }

        if (LastDay < today)
        {
            throw new DomainException(PriceChangeErrors.PeriodEnded);
        }

        if (name.Length == 0 || name.Length > MaxTextLength)
        {
            throw new DomainException(PriceChangeErrors.RevertedByInvalid);
        }

        State = PriceChangeState.Reverted;
        RevertedBy = name;
        RevertedAt = revertedAt.ToUniversalTime();

        // A change that has not begun counts for no day; one in force counted up to yesterday.
        EndedOn = today < FirstDay ? null : today.AddDays(-1);
    }
}
