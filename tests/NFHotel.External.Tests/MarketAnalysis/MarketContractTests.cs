using System.Text.Json;

using NFHotel.Application.Dto.MarketAnalysis;

namespace NFHotel.External.Tests.MarketAnalysis;

/// <summary>
/// The contract of the market analysis service (<c>openapi/market-contract.yaml</c> of <c>notebook-market-analysis</c>) against
/// what the client sends and expects. <c>docs/test-data/market-contract.yaml</c> is a byte-identical copy of that file; the
/// fake of the service reads the booking schema from it, so these tests keep the client, the fake and the contract together.
/// When the service changes its contract, copy the file again and these tests show what the client must follow.
/// </summary>
public sealed class MarketContractTests
{
    private static readonly string[] RunPaths =
    [
        "/api/v1/market-analysis/runs",
        "/api/v1/market-analysis/runs/{run_id}",
        "/api/v1/market-analysis/runs/{run_id}/result",
    ];

    /// <summary>Positive: the three run paths that the client calls are paths of the contract.</summary>
    [Fact]
    public void Contract_RunPaths_AreTheThree()
    {
        // Act
        var paths = MarketContractFile.Load().Paths();

        // Assert
        Assert.All(RunPaths, path => Assert.Contains(path, paths));
    }

    /// <summary>Positive: the run request needs the room count and the bookings, and may have a period.</summary>
    [Fact]
    public void RunRequest_Schema_NeedsRoomCountAndBookings()
    {
        // Act
        var contract = MarketContractFile.Load();

        // Assert
        Assert.Equal(["room_count", "bookings"], contract.Required("RunRequest"));
        Assert.Equal(["room_count", "period", "bookings"], contract.Properties("RunRequest"));
    }

    /// <summary>Positive: the booking of the contract needs these seven fields.</summary>
    [Fact]
    public void Booking_RequiredFields_AreTheSevenOfTheDto()
    {
        // Act
        var required = MarketContractFile.Load().Required("Booking");

        // Assert
        Assert.Equal(
            ["booking_id", "hotel", "is_canceled", "arrival_date", "stays_in_weekend_nights", "stays_in_week_nights", "country"],
            required);
    }

    /// <summary>
    /// Positive: the fields that <see cref="MarketBookingDto"/> writes are exactly the properties of the booking of the contract,
    /// and a booking with every field known has them all, so a field added to either side shows here.
    /// </summary>
    [Fact]
    public void Booking_DtoFields_AreThePropertiesOfTheContract()
    {
        // Arrange
        var booking = new MarketBookingDto(7, "NF Hotel", true, 30, new DateOnly(2019, 1, 30), new DateOnly(2019, 3, 1), 1, 2, 2, 0, 0, "BB", "Portugal", "Direct", false, 0, "C", 0, "No Deposit", "Contract", 0, 1, 94.50m);
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

        // Act
        using var written = JsonDocument.Parse(JsonSerializer.Serialize(booking, options));
        var sent = written.RootElement.EnumerateObject().Select(property => property.Name).Order().ToArray();

        // Assert
        Assert.Equal(MarketContractFile.Load().Properties("Booking").Order(), sent);
    }

    /// <summary>Positive: the result of the contract has the nine analyses that the mapper reads, in this order.</summary>
    [Fact]
    public void RunResult_Schema_HasTheNineAnalyses()
    {
        // Act
        var analyses = MarketContractFile.Load().NestedRequired("RunResult", "analyses");

        // Assert
        Assert.Equal(
            ["lead_time", "holidays", "seasonality", "cancellations", "room_value", "guest_mix", "occupancy", "occupancy_on_holidays", "correlations"],
            analyses);
    }
}
