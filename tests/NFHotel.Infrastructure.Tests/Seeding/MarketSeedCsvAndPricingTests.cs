using NFHotel.Infrastructure.Seeding;

namespace NFHotel.Infrastructure.Tests.Seeding;

/// <summary>Reading <c>nf_hotel_bookings.csv</c> and making its prices (MIL-026).</summary>
public sealed class MarketSeedCsvAndPricingTests
{
    private const string Header =
        "booking_id;hotel;is_canceled;lead_time;arrival_date_week_number;booking_date;arrival_date;arrival_date_day_of_month;stays_in_weekend_nights;stays_in_week_nights;adults;children;babies;meal;country;market_segment;is_repeated_guest;previous_cancellations;assigned_room_type;booking_changes;deposit_type;agent;customer_type;required_car_parking_spaces;total_of_special_requests";

    /// <summary>Positive: a row of the file is read into its fields, with the nights added.</summary>
    [Fact]
    public void Parse_OneRow_ReadsEveryField()
    {
        // Arrange
        var text = Header + "\n1;NF Hotel;1;342;27;2017-07-24;2018-07-01;1;2;3;2;1;0;BB;Portugal;Direct;1;2;C;3;No Deposit;0;No Contract (Single);1;4\n";

        // Act
        var row = Assert.Single(MarketSeedCsv.Parse(new StringReader(text)));

        // Assert
        Assert.Equal((1, true, new DateOnly(2017, 7, 24), new DateOnly(2018, 7, 1), 5), (row.Number, row.IsCanceled, row.BookedAt, row.Arrival, row.Nights));
        Assert.Equal((2, 1, 0, "BB", "Portugal", "Direct", true, 2, "C", 3, "No Deposit", "No Contract (Single)", 1, 4),
            (row.Adults, row.Children, row.Babies, row.Meal, row.Country, row.MarketSegment, row.IsRepeatedGuest, row.PreviousCancellations, row.RoomType, row.BookingChanges, row.DepositType, row.CustomerType, row.ParkingSpaces, row.SpecialRequests));
        Assert.Equal(new DateOnly(2018, 7, 6), row.Departure);
    }

    /// <summary>Boundary: a booking of 0 nights becomes 1 night, because the database has no booking without a night; empty lines are skipped.</summary>
    [Fact]
    public void Parse_ZeroNightsAndBlankLine_GivesOneNightAndSkipsTheBlankLine()
    {
        // Arrange
        var text = Header + "\n\n1;NF Hotel;0;0;27;2018-07-01;2018-07-01;1;0;0;2;0;0;BB;Portugal;Direct;0;0;C;0;No Deposit;0;X;0;0\n";

        // Act
        var row = Assert.Single(MarketSeedCsv.Parse(new StringReader(text)));

        // Assert
        Assert.Equal(1, row.Nights);
    }

    /// <summary>Negative: a header without a needed column names it.</summary>
    [Fact]
    public void Parse_MissingColumn_NamesTheColumn()
    {
        // Act
        var exception = Assert.Throws<FormatException>(() => MarketSeedCsv.Parse(new StringReader("booking_id;hotel\n1;x\n")));

        // Assert
        Assert.Contains("arrival_date", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Negative: a value that cannot be read names the line; an empty file and a short row are refused.</summary>
    [Fact]
    public void Parse_BadValueShortRowAndEmptyFile_AreRefusedWithTheLine()
    {
        // Arrange
        var badDate = Header + "\n1;NF Hotel;0;0;27;01-07-2018;2018-07-01;1;0;0;2;0;0;BB;Portugal;Direct;0;0;C;0;No Deposit;0;X;0;0\n";
        var shortRow = Header + "\n1;NF Hotel;0\n";

        // Act and assert
        Assert.Contains("Line 2", Assert.Throws<FormatException>(() => MarketSeedCsv.Parse(new StringReader(badDate))).Message, StringComparison.Ordinal);
        Assert.Contains("Line 2", Assert.Throws<FormatException>(() => MarketSeedCsv.Parse(new StringReader(shortRow))).Message, StringComparison.Ordinal);
        Assert.Throws<FormatException>(() => MarketSeedCsv.Parse(new StringReader(string.Empty)));
    }

    /// <summary>Positive: the price goes up with the room type letter and with the season, and does not change between calls.</summary>
    [Theory]
    [InlineData("A", 1, 70.00)]
    [InlineData("C", 1, 94.00)]
    [InlineData("C", 5, 108.10)]
    [InlineData("C", 7, 126.90)]
    public void PricePerNight_RoomTypeAndMonth_FollowsTheRule(string roomType, int month, double expected)
    {
        // Arrange
        var row = MarketSeedPlannerTests.Row(1, 0, 1, roomType: roomType) with { Arrival = new DateOnly(2019, month, 10) };

        // Act and assert
        Assert.Equal((decimal)expected, MarketSeedPricing.PricePerNight(row));
        Assert.Equal(MarketSeedPricing.PricePerNight(row), MarketSeedPricing.PricePerNight(row));
    }

    /// <summary>Boundary: a complementary booking costs nothing, and an unknown room type takes the base price.</summary>
    [Fact]
    public void PricePerNight_ComplementaryAndUnknownType_AreZeroAndBase()
    {
        // Assert
        Assert.Equal(0m, MarketSeedPricing.PricePerNight(MarketSeedPlannerTests.Row(1, 0, 1, segment: "Complementary")));
        Assert.Equal(70m, MarketSeedPricing.PricePerNight(MarketSeedPlannerTests.Row(1, 40, 1, roomType: "?") with { Arrival = new DateOnly(2019, 1, 1) }));
    }
}
