using System.ComponentModel.DataAnnotations;

using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Entities;

/// <summary>
/// The nightly base rate of one room size (MIL-051). There is at most one row per size. A Manager sets it; a room has
/// no price of its own.
/// </summary>
public sealed class RoomRate : Entity<int>
{
    /// <summary>The longest user name that is stored.</summary>
    public const int MaxTextLength = 100;

    /// <summary>The largest nightly price.</summary>
    public const double MaxNightlyPrice = BookingMarketDetail.MaxPricePerNight;

    /// <summary>The number of decimals the price is kept with.</summary>
    public const int PriceDecimals = 2;

    private RoomRate()
    {
    }

    /// <summary>The room size the rate is for.</summary>
    [EnumDataType(typeof(RoomSize), ErrorMessage = RoomRateErrors.SizeInvalid)]
    public RoomSize Size { get; private set; }

    /// <summary>The price of one night, above 0 and at most <see cref="MaxNightlyPrice"/>, with two decimals.</summary>
    [Range(0.01, MaxNightlyPrice, ErrorMessage = RoomRateErrors.PriceInvalid)]
    public decimal NightlyPrice { get; private set; }

    /// <summary>The user name of the staff member who set the rate.</summary>
    [Required(ErrorMessage = RoomRateErrors.SetByInvalid)]
    [StringLength(MaxTextLength, ErrorMessage = RoomRateErrors.SetByInvalid)]
    public string SetBy { get; private set; } = string.Empty;

    /// <summary>When the rate was set.</summary>
    public DateTimeOffset SetAt { get; private set; }

    /// <summary>Creates the rate of a room size.</summary>
    /// <param name="size">The room size.</param>
    /// <param name="nightlyPrice">The price of one night; it is rounded to two decimals.</param>
    /// <param name="setBy">Who sets it.</param>
    /// <param name="setAt">When it is set.</param>
    /// <returns>The new rate.</returns>
    /// <exception cref="DomainException">A field breaks an invariant.</exception>
    public static RoomRate Create(RoomSize size, decimal nightlyPrice, string setBy, DateTimeOffset setAt)
    {
        var created = new RoomRate();
        created.Apply(size, nightlyPrice, setBy, setAt);

        return created;
    }

    /// <summary>Changes the price of the rate.</summary>
    /// <param name="nightlyPrice">The price of one night; it is rounded to two decimals.</param>
    /// <param name="setBy">Who changes it.</param>
    /// <param name="setAt">When it is changed.</param>
    /// <exception cref="DomainException">A field breaks an invariant; the rate is not changed.</exception>
    public void Change(decimal nightlyPrice, string setBy, DateTimeOffset setAt) =>
        Apply(Size, nightlyPrice, setBy, setAt);

    private void Apply(RoomSize size, decimal nightlyPrice, string setBy, DateTimeOffset setAt)
    {
        var candidate = new RoomRate
        {
            Size = size,
            NightlyPrice = Math.Round(nightlyPrice, PriceDecimals, MidpointRounding.AwayFromZero),
            SetBy = setBy?.Trim() ?? string.Empty,
            SetAt = setAt.ToUniversalTime(),
        };

        EntityValidation.ThrowIfInvalid(candidate);

        Size = candidate.Size;
        NightlyPrice = candidate.NightlyPrice;
        SetBy = candidate.SetBy;
        SetAt = candidate.SetAt;
    }
}
