using NFHotel.Application.Dto.Pricing;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Pricing;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;
using NFHotel.Web.Features.Admin.Rooms;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>The presentation logic of the room list: the price column that only a user who may manage prices sees (MIL-055).</summary>
public sealed class RoomListViewModelTests
{
    private static readonly DateOnly Today = new(2026, 11, 2);

    #region Prices for a user who may manage prices

    /// <summary>Positive: base rate 100 with a +20 % change in force is shown as the move and the change; no change shows the rate alone.</summary>
    [Fact]
    public async Task InitializeAsync_CanSeePrices_ShowsTheEffectivePricePerSize()
    {
        // Arrange
        var (viewModel, _, _) = Build();

        // Act
        await viewModel.InitializeAsync(true, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(viewModel.ShowsPrices);
        Assert.Equal("100.00 → 120.00 (+20 %)", viewModel.PriceTextOf(RoomSize.Single));
        Assert.Equal("150.00", viewModel.PriceTextOf(RoomSize.Double));
        Assert.Empty(viewModel.Errors);
        Assert.Equal(2, viewModel.Rooms.Count);
    }

    /// <summary>Negative: a size without a base rate says "not set".</summary>
    [Fact]
    public async Task PriceTextOf_SizeWithoutBaseRate_IsNotSet()
    {
        // Arrange
        var (viewModel, _, _) = Build();
        await viewModel.InitializeAsync(true, TestContext.Current.CancellationToken);

        // Act
        var text = viewModel.PriceTextOf(RoomSize.Suite);

        // Assert
        Assert.Equal("not set", text);
    }

    /// <summary>Negative: a discount is shown with a minus sign.</summary>
    [Fact]
    public async Task InitializeAsync_DiscountInForce_ShowsTheMinusSign()
    {
        // Arrange
        var (viewModel, _, prices) = Build();
        prices.GetEffectivePricesAsync(Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>()).Returns(
            Result<IReadOnlyList<EffectivePriceDto>>.Success([new EffectivePriceDto("Single", Today, 100m, 90m, -10m)]));

        // Act
        await viewModel.InitializeAsync(true, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("100.00 → 90.00 (−10 %)", viewModel.PriceTextOf(RoomSize.Single));
    }

    /// <summary>Positive: the prices of today are asked for once, in one call for all sizes, and not again by a search or a clear.</summary>
    [Fact]
    public async Task InitializeAsync_CanSeePrices_AsksForTodaysPricesOnce()
    {
        // Arrange
        var (viewModel, _, prices) = Build();

        // Act
        await viewModel.InitializeAsync(true, TestContext.Current.CancellationToken);
        await viewModel.SearchAsync(TestContext.Current.CancellationToken);
        await viewModel.ClearFilterAsync(TestContext.Current.CancellationToken);

        // Assert
        await prices.Received(1).GetEffectivePricesAsync(Today, Arg.Any<CancellationToken>());
        await prices.Received(1).GetEffectivePricesAsync(Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Negative: a price service that is down leaves the rooms listed and shows the text of the code.</summary>
    [Fact]
    public async Task InitializeAsync_PriceServiceDown_KeepsTheRoomsAndShowsTheError()
    {
        // Arrange
        var (viewModel, _, prices) = Build();
        prices.GetEffectivePricesAsync(Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<EffectivePriceDto>>.Failure(ErrorCodes.Api.Unavailable));

        // Act
        await viewModel.InitializeAsync(true, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, viewModel.Rooms.Count);
        Assert.Single(viewModel.Errors);
        Assert.Equal("not set", viewModel.PriceTextOf(RoomSize.Single));
    }

    #endregion

    #region No prices for a user who may not manage prices

    /// <summary>Negative: without the permission the price column is hidden and the price service is never called.</summary>
    [Fact]
    public async Task InitializeAsync_CannotSeePrices_HidesThePricesAndDoesNotAskForThem()
    {
        // Arrange
        var (viewModel, rooms, prices) = Build();

        // Act
        await viewModel.InitializeAsync(false, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(viewModel.ShowsPrices);
        Assert.Equal(2, viewModel.Rooms.Count);
        Assert.Empty(viewModel.Errors);
        Assert.Empty(prices.ReceivedCalls());
        await rooms.Received(1).SearchAsync(Arg.Any<RoomFilter>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helpers

    private static (RoomListViewModel ViewModel, IRoomService Rooms, IPriceSuggestionService Prices) Build()
    {
        var rooms = Substitute.For<IRoomService>();
        rooms.GetFilterOptionsAsync(Arg.Any<CancellationToken>()).Returns(
            Result<RoomFilterOptionsDto>.Success(new RoomFilterOptionsDto([1], Enum.GetValues<RoomSize>(), Enum.GetValues<RoomStatus>(), Enum.GetValues<HousekeepingStatus>())));
        rooms.SearchAsync(Arg.Any<RoomFilter>(), Arg.Any<CancellationToken>()).Returns(
            Result<IReadOnlyList<RoomListItemDto>>.Success(
            [
                new RoomListItemDto(1, "101", 1, RoomSize.Single, 1, RoomStatus.Available, HousekeepingStatus.Clean),
                new RoomListItemDto(2, "102", 1, RoomSize.Double, 2, RoomStatus.Available, HousekeepingStatus.Clean),
            ]));

        var prices = Substitute.For<IPriceSuggestionService>();
        prices.GetEffectivePricesAsync(Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>()).Returns(
            Result<IReadOnlyList<EffectivePriceDto>>.Success(
            [
                new EffectivePriceDto("Single", Today, 100m, 120m, 20m),
                new EffectivePriceDto("Double", Today, 150m, 150m, null),
                new EffectivePriceDto("Suite", Today, null, null, null),
            ]));

        var viewModel = new RoomListViewModel(
            new PassThroughRunner<IRoomService>(rooms),
            new PassThroughRunner<IPriceSuggestionService>(prices),
            new FakeClock { Today = Today });

        return (viewModel, rooms, prices);
    }

    #endregion
}
