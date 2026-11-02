using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.MarketAnalysis;

/// <summary>The market details of a booking and the rules of their fields (MIL-030, MIL-054 task 8).</summary>
public sealed class BookingMarketDetailTests
{
    private static readonly DateOnly BookedAt = new(2026, 1, 15);

    private static readonly BookingMarketValues Valid =
        new(94.50m, 2, 1, 0, " BB ", "Direct", "Transient", "No Deposit", "C", true, 1, 2, 3, 1);

    /// <summary>Positive: valid values are stored, and the texts are trimmed.</summary>
    [Fact]
    public void Create_ValidValues_StoresThemTrimmed()
    {
        // Act
        var detail = BookingMarketDetail.Create(7, BookedAt, Valid);

        // Assert
        Assert.Equal((7, BookedAt, 94.50m, 2, 1, 0), (detail.BookingId, detail.BookedAt, detail.PricePerNight, detail.Adults, detail.Children, detail.Babies));
        Assert.Equal(("BB", "Direct", "Transient", "No Deposit", "C"), (detail.Meal, detail.MarketSegment, detail.CustomerType, detail.DepositType, detail.RoomType));
        Assert.Equal((true, 1, 2, 3, 1), (detail.IsRepeatedGuest, detail.PreviousCancellations, detail.BookingChanges, detail.SpecialRequests, detail.ParkingSpaces));
    }

    /// <summary>Edge: an empty or blank text and a missing price are stored as no value.</summary>
    [Fact]
    public void Create_BlankTextsAndNoPrice_StoresNull()
    {
        // Act
        var detail = BookingMarketDetail.Create(7, BookedAt, Valid with { PricePerNight = null, Meal = "  ", RoomType = null });

        // Assert
        Assert.Null(detail.PricePerNight);
        Assert.Null(detail.Meal);
        Assert.Null(detail.RoomType);
    }

    /// <summary>Boundary: the largest count is allowed, one more is refused.</summary>
    [Theory]
    [InlineData(BookingMarketDetail.MaxCount, true)]
    [InlineData(BookingMarketDetail.MaxCount + 1, false)]
    [InlineData(-1, false)]
    public void Create_Count_IsCheckedAtTheLimit(int count, bool allowed)
    {
        // Act
        var exception = Record.Exception(() => BookingMarketDetail.Create(7, BookedAt, Valid with { Adults = count }));

        // Assert
        if (allowed)
        {
            Assert.Null(exception);
        }
        else
        {
            Assert.Contains(BookingMarketDetailErrors.CountInvalid, Assert.IsType<DomainException>(exception).Codes);
        }
    }

    /// <summary>Negative: every broken rule is reported at once, each code once.</summary>
    [Fact]
    public void Create_SeveralBrokenRules_ReportsEachCodeOnce()
    {
        // Act
        var exception = Assert.Throws<DomainException>(() => BookingMarketDetail.Create(
            0,
            BookedAt,
            Valid with { PricePerNight = -1m, Adults = -1, Babies = -1, Meal = new string('x', BookingMarketDetail.MaxTextLength + 1) }));

        // Assert
        Assert.Equal(
            new[] { BookingMarketDetailErrors.BookingRequired, BookingMarketDetailErrors.CountInvalid, BookingMarketDetailErrors.PriceInvalid, BookingMarketDetailErrors.TextInvalid }.Order(StringComparer.Ordinal),
            exception.Codes.Order(StringComparer.Ordinal));
    }

    /// <summary>Negative: values are required.</summary>
    [Fact]
    public void Create_NoValues_Throws() =>
        Assert.Throws<ArgumentNullException>(() => BookingMarketDetail.Create(7, BookedAt, null!));

    /// <summary>Positive: a domain error can carry the error that caused it.</summary>
    [Fact]
    public void DomainException_WithInnerException_KeepsCodeAndCause()
    {
        // Arrange
        var cause = new FormatException("bad");

        // Act
        var exception = new DomainException(BookingMarketDetailErrors.TextInvalid, cause);

        // Assert
        Assert.Equal(BookingMarketDetailErrors.TextInvalid, exception.Code);
        Assert.Equal([BookingMarketDetailErrors.TextInvalid], exception.Codes);
        Assert.Same(cause, exception.InnerException);
    }
}
