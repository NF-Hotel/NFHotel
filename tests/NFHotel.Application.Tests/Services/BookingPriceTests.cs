using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Pricing;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Pricing;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

using NSubstitute;

namespace NFHotel.Application.Tests.Services;

/// <summary>The price per night that a new booking is given from the effective price of its room size on the arrival date (MIL-055).</summary>
public sealed class BookingPriceTests
{
    private static readonly DateOnly Today = new(2027, 5, 1);
    private static readonly DateOnly Arrival = new(2027, 6, 10);
    private static readonly DateOnly OtherArrival = new(2027, 9, 10);

    #region Creating

    /// <summary>Positive: base rate 100 and a +20 % change that covers the arrival gives 120.00, written after the booking is saved.</summary>
    [Fact]
    public async Task CreateAsync_ChangeCoversTheArrival_StoresTheChangedPrice()
    {
        // Arrange
        var (world, room) = await WorldWithChangeAsync(20m);

        // Act
        var result = await world.BookingService(AllowAll()).CreateAsync(ExistingGuestRequest(world, room, Arrival), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var detail = Assert.Single(world.BookingMarketDetails.Items);
        Assert.Equal((world.Bookings.Items.Single().Id, 120.00m, Today), (detail.BookingId, detail.PricePerNight, detail.BookedAt));
        Assert.Equal((0, 0, 0), (detail.Adults, detail.Children, detail.Babies));
        Assert.Null(detail.MarketSegment);
        Assert.Null(detail.Meal);
        Assert.Null(detail.CustomerType);
        Assert.Null(detail.DepositType);
        Assert.Equal(2, world.UnitOfWork.SaveCount);
    }

    /// <summary>Boundary: an arrival outside the period of the change gets the base rate 100.00.</summary>
    [Fact]
    public async Task CreateAsync_ArrivalOutsideTheChange_StoresTheBaseRate()
    {
        // Arrange
        var (world, room) = await WorldWithChangeAsync(20m);

        // Act
        var result = await world.BookingService(AllowAll()).CreateAsync(ExistingGuestRequest(world, room, OtherArrival), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(100.00m, Assert.Single(world.BookingMarketDetails.Items).PricePerNight);
    }

    /// <summary>Boundary: the price is that of the size of the booked room; a double room has its own rate.</summary>
    [Fact]
    public async Task CreateAsync_DoubleRoom_StoresTheRateOfTheDoubleSize()
    {
        // Arrange
        var (world, _) = await WorldWithChangeAsync(20m);
        var doubleRoom = world.AddRoom("201", 2, RoomSize.Double);

        // Act
        await world.BookingService(AllowAll()).CreateAsync(ExistingGuestRequest(world, doubleRoom, Arrival), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(150.00m, Assert.Single(world.BookingMarketDetails.Items).PricePerNight);
    }

    /// <summary>Negative: a size without a base rate leaves the booking as before: created, one save, no detail row.</summary>
    [Fact]
    public async Task CreateAsync_NoBaseRateForTheSize_CreatesTheBookingWithoutADetail()
    {
        // Arrange
        var world = NewWorld();
        var suite = world.AddRoom("301", 3, RoomSize.Suite);

        // Act
        var result = await world.BookingService(AllowAll()).CreateAsync(ExistingGuestRequest(world, suite, Arrival), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(world.Bookings.Items);
        Assert.Empty(world.BookingMarketDetails.Items);
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: an error from the price lookup does not stop the booking; it is created without a price.</summary>
    [Fact]
    public async Task CreateAsync_PriceLookupFails_CreatesTheBookingWithoutADetail()
    {
        // Arrange
        var world = NewWorld();
        var room = world.AddRoom("101", 1, RoomSize.Single);

        // Act
        var result = await world.BookingService(AllowAll(), FailingPrices()).CreateAsync(ExistingGuestRequest(world, room, Arrival), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(world.Bookings.Items);
        Assert.Empty(world.BookingMarketDetails.Items);
    }

    /// <summary>Negative: if the detail cannot be saved the booking stays, and the detail is taken back so no later save retries it.</summary>
    [Fact]
    public async Task CreateAsync_SavingTheDetailFails_KeepsTheBookingAndDiscardsTheDetail()
    {
        // Arrange
        var (world, room) = await WorldWithChangeAsync(20m);
        world.UnitOfWork.FailOnSaveNumber = 2;

        // Act
        var result = await world.BookingService(AllowAll()).CreateAsync(ExistingGuestRequest(world, room, Arrival), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(world.Bookings.Items);
        Assert.Empty(world.BookingMarketDetails.Items);
        Assert.Single(world.BookingMarketDetails.Discarded);
    }

    /// <summary>Negative: a lost price is reported once as a warning with the booking id and the exception, and nothing about the guest.</summary>
    [Fact]
    public async Task CreateAsync_SavingTheDetailFails_WritesAWarningWithTheBookingIdAndTheException()
    {
        // Arrange
        var (world, room) = await WorldWithChangeAsync(20m);
        world.UnitOfWork.FailOnSaveNumber = 2;

        // Act
        var result = await world.BookingService(AllowAll()).CreateAsync(ExistingGuestRequest(world, room, Arrival), TestContext.Current.CancellationToken);

        // Assert
        var bookingId = result.Value.BookingId;
        var warning = Assert.Single(world.Warnings.Messages);
        Assert.Contains($"booking {bookingId} ", warning, StringComparison.Ordinal);
        Assert.Contains(nameof(InvalidOperationException), warning, StringComparison.Ordinal);
    }

    /// <summary>Positive: a stored price writes no warning.</summary>
    [Fact]
    public async Task CreateAsync_DetailIsSaved_WritesNoWarning()
    {
        // Arrange
        var (world, room) = await WorldWithChangeAsync(20m);

        // Act
        await world.BookingService(AllowAll()).CreateAsync(ExistingGuestRequest(world, room, Arrival), TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(world.Warnings.Messages);
    }

    /// <summary>Negative: a booking that is refused (overlap) stores no detail.</summary>
    [Fact]
    public async Task CreateAsync_RoomNotAvailable_StoresNoDetail()
    {
        // Arrange
        var (world, room) = await WorldWithChangeAsync(20m);
        var guest = world.AddGuest();
        world.AddBooking(room.Id, guest.Id, Arrival.DayNumber - Today.DayNumber - 1, 4);

        // Act
        var result = await world.BookingService(AllowAll()).CreateAsync(ExistingGuestRequest(world, room, Arrival), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Empty(world.BookingMarketDetails.Items);
    }

    #endregion

    #region The public flow

    /// <summary>Positive: the public booking flow stores the same price as the staff flow.</summary>
    [Fact]
    public async Task CreatePublicAsync_ChangeCoversTheArrival_StoresTheChangedPrice()
    {
        // Arrange
        var (world, room) = await WorldWithChangeAsync(20m);
        var request = new CreateBookingRequest(Arrival, Arrival.AddDays(2), room.Id, null, ServiceWorld.ValidGuestFields());

        // Act
        var result = await world.BookingService().CreatePublicAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        var detail = Assert.Single(world.BookingMarketDetails.Items);
        Assert.Equal((world.Bookings.Items.Single().Id, 120.00m), (detail.BookingId, detail.PricePerNight));
    }

    /// <summary>Negative: the public flow with no base rate, or with a failing lookup, still creates the booking, without a detail.</summary>
    [Fact]
    public async Task CreatePublicAsync_NoPrice_CreatesTheBookingWithoutADetail()
    {
        // Arrange
        var world = NewWorld();
        var room = world.AddRoom("101", 1, RoomSize.Single);
        var request = new CreateBookingRequest(Arrival, Arrival.AddDays(2), room.Id, null, ServiceWorld.ValidGuestFields());
        var later = request with { CheckInDate = Arrival.AddDays(10), CheckOutDate = Arrival.AddDays(12) };

        // Act
        var withoutRate = await world.BookingService().CreatePublicAsync(request, TestContext.Current.CancellationToken);
        var failed = await world.BookingService(null, FailingPrices()).CreatePublicAsync(later, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(withoutRate.IsSuccess);
        Assert.True(failed.IsSuccess);
        Assert.Equal(2, world.Bookings.Items.Count);
        Assert.Empty(world.BookingMarketDetails.Items);
    }

    #endregion

    #region A stored price stays

    /// <summary>Positive: a price change approved after the booking does not alter the stored price of the booking.</summary>
    [Fact]
    public async Task ApproveAsync_AfterTheBooking_LeavesTheStoredPriceAlone()
    {
        // Arrange
        var (world, room) = await WorldWithChangeAsync(20m);
        await world.BookingService(AllowAll()).CreateAsync(ExistingGuestRequest(world, room, Arrival), TestContext.Current.CancellationToken);
        var detail = Assert.Single(world.BookingMarketDetails.Items);

        // Act
        var later = await world.PriceChangeService().ApproveAsync("Single", 50m, new DateOnly(2027, 9, 1), new DateOnly(2027, 9, 30), TestContext.Current.CancellationToken);
        var now = await world.PriceChangeService().GetEffectivePriceAsync("Single", OtherArrival, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(later.IsSuccess);
        Assert.Equal(150.00m, now.Value.Price);
        Assert.Same(detail, Assert.Single(world.BookingMarketDetails.Items));
        Assert.Equal(120.00m, detail.PricePerNight);
        Assert.Empty(world.BookingMarketDetails.Updated);
    }

    /// <summary>Positive: confirm, reschedule to a day with another price, and cancel keep the stored price and add no row.</summary>
    [Fact]
    public async Task ConfirmRescheduleAndCancel_AfterTheBooking_KeepTheStoredPrice()
    {
        // Arrange
        var (world, room) = await WorldWithChangeAsync(20m);
        var service = world.BookingService(AllowAll());
        var created = await service.CreateAsync(ExistingGuestRequest(world, room, Arrival), TestContext.Current.CancellationToken);
        var detail = Assert.Single(world.BookingMarketDetails.Items);
        var bookingId = created.Value.BookingId;

        // Act
        var confirmed = await service.ConfirmAsync(bookingId, TestContext.Current.CancellationToken);
        var moved = await service.RescheduleAsync(new RescheduleBookingRequest(bookingId, OtherArrival, OtherArrival.AddDays(2), room.Id), TestContext.Current.CancellationToken);
        var cancelled = await service.CancelAsync(bookingId, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(confirmed.IsSuccess);
        Assert.True(moved.IsSuccess);
        Assert.True(cancelled.IsSuccess);
        Assert.Same(detail, Assert.Single(world.BookingMarketDetails.Items));
        Assert.Equal(120.00m, detail.PricePerNight);
        Assert.Empty(world.BookingMarketDetails.Updated);
        Assert.Empty(world.BookingMarketDetails.Removed);
    }

    #endregion

    #region Helpers

    private static ServiceWorld NewWorld()
    {
        var world = new ServiceWorld();
        world.CallContext.StaffId = 7;
        world.CallContext.StaffName = "manager";
        world.Clock.Today = Today;
        return world;
    }

    private static async Task<(ServiceWorld World, Room Room)> WorldWithChangeAsync(decimal percentage)
    {
        var world = NewWorld();
        world.RoomRates.Add(RoomRate.Create(RoomSize.Single, 100m, "manager", world.Clock.UtcNow));
        world.RoomRates.Add(RoomRate.Create(RoomSize.Double, 150m, "manager", world.Clock.UtcNow));
        var room = world.AddRoom("101", 1, RoomSize.Single);
        var approved = await world.PriceChangeService().ApproveAsync("Single", percentage, new DateOnly(2027, 6, 1), new DateOnly(2027, 8, 31), TestContext.Current.CancellationToken);
        Assert.True(approved.IsSuccess);
        world.UnitOfWork.ResetSaveCount();
        return (world, room);
    }

    private static CreateBookingRequest ExistingGuestRequest(ServiceWorld world, Room room, DateOnly arrival) =>
        new(arrival, arrival.AddDays(2), room.Id, world.AddGuest().Id, null);

    private static IPriceChangeService FailingPrices()
    {
        var prices = Substitute.For<IPriceChangeService>();
        prices.GetEffectivePriceAsync(Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<EffectivePriceDto>.Failure("price.lookup_failed")));
        return prices;
    }

    private static IStaffAuthorizationService AllowAll()
    {
        var authorization = Substitute.For<IStaffAuthorizationService>();
        authorization.HasPermissionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));
        return authorization;
    }

    #endregion
}
