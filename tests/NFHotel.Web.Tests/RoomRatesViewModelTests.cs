using NFHotel.Application.Dto.RoomRates;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.RoomRates;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Features.Admin.Pricing;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>The presentation logic of the room rates page (MIL-051).</summary>
public sealed class RoomRatesViewModelTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);

    /// <summary>Positive: the rates are loaded, shown with two decimals in their fields, and a size without a rate has none.</summary>
    [Fact]
    public async Task LoadAsync_OneRateSet_ShowsItAndLeavesTheOthersUnset()
    {
        // Arrange
        var (viewModel, _) = Build();

        // Act
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(viewModel.IsLoaded);
        Assert.Equal("manager", viewModel.RateOf(RoomSize.Single)!.SetBy);
        Assert.Equal("80.00", viewModel.InputOf(RoomSize.Single));
        Assert.Null(viewModel.RateOf(RoomSize.Double));
        Assert.Null(viewModel.RateOf(RoomSize.Suite));
        Assert.Empty(viewModel.Errors);
    }

    /// <summary>Negative: a service that is down shows the text of the code.</summary>
    [Fact]
    public async Task LoadAsync_ServiceDown_ShowsTheText()
    {
        // Arrange
        var (viewModel, service) = Build();
        service.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<RoomRateDto>>.Failure(ErrorCodes.Api.Unavailable));

        // Act
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(viewModel.IsLoaded);
        Assert.Single(viewModel.Errors);
        Assert.Null(viewModel.RateOf(RoomSize.Single));
    }

    /// <summary>Positive: Save sends the size and the typed price, shows the new rate and says so.</summary>
    [Fact]
    public async Task SaveAsync_ValidPrice_SendsItAndShowsTheNewRate()
    {
        // Arrange
        var (viewModel, service) = Build();
        service.SetAsync("Double", Arg.Any<SetRoomRateRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<RoomRateDto>.Success(new RoomRateDto("Double", 120.5m, "manager", At)));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.SetInput(RoomSize.Double, "120.5");

        // Act
        await viewModel.SaveAsync(RoomSize.Double, TestContext.Current.CancellationToken);

        // Assert
        await service.Received(1).SetAsync("Double", Arg.Is<SetRoomRateRequest>(request => request.NightlyPrice == 120.5m), Arg.Any<CancellationToken>());
        Assert.Equal(120.5m, viewModel.RateOf(RoomSize.Double)!.NightlyPrice);
        Assert.Equal("120.50", viewModel.InputOf(RoomSize.Double));
        Assert.Equal("The rate of Double is now 120.50 a night.", viewModel.SuccessMessage);
        Assert.Empty(viewModel.Errors);
    }

    /// <summary>Negative: text that is not a number is refused with the price text, and the service is not called.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("12,50x")]
    public async Task SaveAsync_NotANumber_ShowsThePriceTextWithoutCalling(string typed)
    {
        // Arrange
        var (viewModel, service) = Build();
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.SetInput(RoomSize.Suite, typed);

        // Act
        await viewModel.SaveAsync(RoomSize.Suite, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["Enter a nightly price above 0 and at most 1,000,000."], viewModel.Errors);
        Assert.Null(viewModel.SuccessMessage);
        await service.DidNotReceive().SetAsync(Arg.Any<string>(), Arg.Any<SetRoomRateRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Negative: a refusal of the API shows the text of its code and keeps the old rate.</summary>
    [Fact]
    public async Task SaveAsync_RefusedByTheApi_ShowsTheTextAndKeepsTheOldRate()
    {
        // Arrange
        var (viewModel, service) = Build();
        service.SetAsync("Single", Arg.Any<SetRoomRateRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<RoomRateDto>.Failure(NFHotel.Domain.Errors.RoomRateErrors.PriceInvalid));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.SetInput(RoomSize.Single, "0");

        // Act
        await viewModel.SaveAsync(RoomSize.Single, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["Enter a nightly price above 0 and at most 1,000,000."], viewModel.Errors);
        Assert.Equal(80m, viewModel.RateOf(RoomSize.Single)!.NightlyPrice);
        Assert.Null(viewModel.SuccessMessage);
    }

    private static (RoomRatesViewModel ViewModel, IRoomRateService Service) Build()
    {
        var service = Substitute.For<IRoomRateService>();
        service.GetAllAsync(Arg.Any<CancellationToken>()).Returns(
            Result<IReadOnlyList<RoomRateDto>>.Success([new RoomRateDto("Single", 80m, "manager", At)]));
        return (new RoomRatesViewModel(new PassThroughRunner<IRoomRateService>(service)), service);
    }
}
