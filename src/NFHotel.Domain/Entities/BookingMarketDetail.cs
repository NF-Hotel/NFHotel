using System.ComponentModel.DataAnnotations;

using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Entities;

/// <summary>
/// What the market analysis needs to know about a booking and the booking itself does not hold (MIL-026): when it was made,
/// the price, who came, how it was sold. It is a table of its own, one row per booking at most, so that the booking and its
/// state machine do not change; a booking without a row is analysed with these fields missing.
/// </summary>
public sealed class BookingMarketDetail : Entity<int>
{
    /// <summary>The longest meal, market segment, customer type, deposit type and room type text.</summary>
    public const int MaxTextLength = 40;

    /// <summary>The largest count of guests, changes, requests and earlier cancellations.</summary>
    public const int MaxCount = 1000;

    /// <summary>The largest price per night.</summary>
    public const double MaxPricePerNight = 1_000_000;

    private BookingMarketDetail()
    {
    }

    /// <summary>The booking the details belong to.</summary>
    public int BookingId { get; private set; }

    /// <summary>The date on which the booking was made; the lead time is the days from it to the arrival.</summary>
    public DateOnly BookedAt { get; private set; }

    /// <summary>The price of one night, or <c>null</c> when it is not known.</summary>
    [Range(0, MaxPricePerNight, ErrorMessage = BookingMarketDetailErrors.PriceInvalid)]
    public decimal? PricePerNight { get; private set; }

    /// <summary>The adults of the booking.</summary>
    [Range(0, MaxCount, ErrorMessage = BookingMarketDetailErrors.CountInvalid)]
    public int Adults { get; private set; }

    /// <summary>The children of the booking.</summary>
    [Range(0, MaxCount, ErrorMessage = BookingMarketDetailErrors.CountInvalid)]
    public int Children { get; private set; }

    /// <summary>The babies of the booking.</summary>
    [Range(0, MaxCount, ErrorMessage = BookingMarketDetailErrors.CountInvalid)]
    public int Babies { get; private set; }

    /// <summary>The meal plan, for example <c>BB</c>, or <c>null</c>.</summary>
    [StringLength(MaxTextLength, ErrorMessage = BookingMarketDetailErrors.TextInvalid)]
    public string? Meal { get; private set; }

    /// <summary>How the booking was sold, for example <c>Direct</c>, or <c>null</c>.</summary>
    [StringLength(MaxTextLength, ErrorMessage = BookingMarketDetailErrors.TextInvalid)]
    public string? MarketSegment { get; private set; }

    /// <summary>The kind of customer, or <c>null</c>.</summary>
    [StringLength(MaxTextLength, ErrorMessage = BookingMarketDetailErrors.TextInvalid)]
    public string? CustomerType { get; private set; }

    /// <summary>The kind of deposit, for example <c>No Deposit</c>, or <c>null</c>.</summary>
    [StringLength(MaxTextLength, ErrorMessage = BookingMarketDetailErrors.TextInvalid)]
    public string? DepositType { get; private set; }

    /// <summary>The room type the guest was given, for example <c>C</c>, or <c>null</c>.</summary>
    [StringLength(MaxTextLength, ErrorMessage = BookingMarketDetailErrors.TextInvalid)]
    public string? RoomType { get; private set; }

    /// <summary>True when the guest has stayed before.</summary>
    public bool IsRepeatedGuest { get; private set; }

    /// <summary>The bookings of this guest that were cancelled earlier.</summary>
    [Range(0, MaxCount, ErrorMessage = BookingMarketDetailErrors.CountInvalid)]
    public int PreviousCancellations { get; private set; }

    /// <summary>The changes made to the booking after it was made.</summary>
    [Range(0, MaxCount, ErrorMessage = BookingMarketDetailErrors.CountInvalid)]
    public int BookingChanges { get; private set; }

    /// <summary>The special requests of the guest.</summary>
    [Range(0, MaxCount, ErrorMessage = BookingMarketDetailErrors.CountInvalid)]
    public int SpecialRequests { get; private set; }

    /// <summary>The car parking spaces the guest asked for.</summary>
    [Range(0, MaxCount, ErrorMessage = BookingMarketDetailErrors.CountInvalid)]
    public int ParkingSpaces { get; private set; }

    /// <summary>Creates the details of a booking and checks every field.</summary>
    /// <param name="bookingId">The booking; greater than 0.</param>
    /// <param name="bookedAt">The date on which the booking was made.</param>
    /// <param name="values">The other fields.</param>
    /// <returns>The new details.</returns>
    /// <exception cref="DomainException">A field breaks an invariant.</exception>
    public static BookingMarketDetail Create(int bookingId, DateOnly bookedAt, BookingMarketValues values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var detail = new BookingMarketDetail
        {
            BookingId = bookingId,
            BookedAt = bookedAt,
            PricePerNight = values.PricePerNight,
            Adults = values.Adults,
            Children = values.Children,
            Babies = values.Babies,
            Meal = Clean(values.Meal),
            MarketSegment = Clean(values.MarketSegment),
            CustomerType = Clean(values.CustomerType),
            DepositType = Clean(values.DepositType),
            RoomType = Clean(values.RoomType),
            IsRepeatedGuest = values.IsRepeatedGuest,
            PreviousCancellations = values.PreviousCancellations,
            BookingChanges = values.BookingChanges,
            SpecialRequests = values.SpecialRequests,
            ParkingSpaces = values.ParkingSpaces,
        };

        var codes = new List<string>();
        if (bookingId <= 0)
        {
            codes.Add(BookingMarketDetailErrors.BookingRequired);
        }

        try
        {
            EntityValidation.ThrowIfInvalid(detail);
        }
        catch (DomainException exception)
        {
            codes.AddRange(exception.Codes);
        }

        if (codes.Count > 0)
        {
            throw new DomainException(codes.Distinct().ToArray());
        }

        return detail;
    }

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
