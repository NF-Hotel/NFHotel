using NFHotel.Application.Dto.Pricing;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Pricing;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Features.Admin.Pricing;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>The presentation logic of the pricing settings page (MIL-051).</summary>
public sealed class PricingSettingsViewModelTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 1, 10, 30, 0, TimeSpan.Zero);

    private static readonly PricingSettingsDto Defaults = new(20m, 15m, 80m, 40m, 30m, 0m, ["Single"], null, null);

    private static readonly PricingSettingsDto Saved = new(25m, 10m, 90m, 30m, 20m, 55.5m, ["Single", "Double"], "manager", At);

    #region Loading

    /// <summary>Positive: the defaults are shown in the fields and the page says they are not saved yet.</summary>
    [Fact]
    public async Task LoadAsync_NothingSaved_ShowsTheDefaultsAndSaysSo()
    {
        // Arrange
        var (viewModel, _) = Build(Defaults);

        // Act
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(viewModel.IsLoaded);
        Assert.True(viewModel.AreDefaults);
        Assert.Equal("Defaults (not saved yet).", viewModel.SavedText);
        Assert.Equal("20.00", viewModel.InputOf(PricingSettingsViewModel.Fields.Surcharge));
        Assert.Equal("0.00", viewModel.InputOf(PricingSettingsViewModel.Fields.MinimumPrice));
        Assert.True(viewModel.IsDiscounted(RoomSize.Single));
        Assert.False(viewModel.IsDiscounted(RoomSize.Double));
        Assert.Empty(viewModel.Errors);
    }

    /// <summary>Positive: saved settings are shown with who saved them and when, and the sizes are checked.</summary>
    [Fact]
    public async Task LoadAsync_SettingsSaved_ShowsThemAndWhoSavedThem()
    {
        // Arrange
        var (viewModel, _) = Build(Saved);

        // Act
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.False(viewModel.AreDefaults);
        Assert.Equal("Last saved by manager at 2026-11-01 10:30 UTC.", viewModel.SavedText);
        Assert.Equal("55.50", viewModel.InputOf(PricingSettingsViewModel.Fields.MinimumPrice));
        Assert.True(viewModel.IsDiscounted(RoomSize.Double));
        Assert.False(viewModel.IsDiscounted(RoomSize.Suite));
    }

    /// <summary>Negative: a service that is down shows the text of the code and still ends the loading.</summary>
    [Fact]
    public async Task LoadAsync_ServiceDown_ShowsTheText()
    {
        // Arrange
        var (viewModel, service) = Build(Defaults);
        service.GetAsync(Arg.Any<CancellationToken>()).Returns(Result<PricingSettingsDto>.Failure(ErrorCodes.Api.Unavailable));

        // Act
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(viewModel.IsLoaded);
        Assert.Single(viewModel.Errors);
        Assert.Null(viewModel.Current);
    }

    #endregion

    #region Saving

    /// <summary>Positive: Save sends the typed numbers and the checked sizes, shows the answer and says so.</summary>
    [Fact]
    public async Task SaveAsync_ValidInput_SendsTheRequestAndShowsTheAnswer()
    {
        // Arrange
        var (viewModel, service) = Build(Defaults);
        service.SetAsync(Arg.Any<SetPricingSettingsRequest>(), Arg.Any<CancellationToken>()).Returns(Result<PricingSettingsDto>.Success(Saved));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.SetInput(PricingSettingsViewModel.Fields.Surcharge, "25");
        viewModel.SetInput(PricingSettingsViewModel.Fields.Discount, "10.5");
        viewModel.SetDiscounted(RoomSize.Suite, true);
        viewModel.SetDiscounted(RoomSize.Single, false);

        // Act
        await viewModel.SaveAsync(TestContext.Current.CancellationToken);

        // Assert
        await service.Received(1).SetAsync(
            Arg.Is<SetPricingSettingsRequest>(request =>
                request.SurchargePercent == 25m
                && request.DiscountPercent == 10.5m
                && request.HighThresholdPercent == 80m
                && request.LowThresholdPercent == 40m
                && request.MaxChangePercent == 30m
                && request.MinimumNightlyPrice == 0m
                && request.DiscountSizes.SequenceEqual(new[] { "Suite" })),
            Arg.Any<CancellationToken>());
        Assert.Equal("The pricing settings are saved.", viewModel.SuccessMessage);
        Assert.Equal("Last saved by manager at 2026-11-01 10:30 UTC.", viewModel.SavedText);
        Assert.Equal("90.00", viewModel.InputOf(PricingSettingsViewModel.Fields.HighThreshold));
        Assert.Empty(viewModel.Errors);
    }

    /// <summary>Negative: text that is not a number is refused with the text of that field, and the service is not called.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("12,5")]
    [InlineData("1e3")]
    public async Task SaveAsync_NotANumber_ShowsTheFieldTextWithoutCalling(string typed)
    {
        // Arrange
        var (viewModel, service) = Build(Defaults);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.SetInput(PricingSettingsViewModel.Fields.Surcharge, typed);

        // Act
        await viewModel.SaveAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["Enter a surcharge from 0 to 100 percent."], viewModel.Errors);
        Assert.Null(viewModel.SuccessMessage);
        await service.DidNotReceive().SetAsync(Arg.Any<SetPricingSettingsRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Negative: several fields that are not numbers are all named at once.</summary>
    [Fact]
    public async Task SaveAsync_SeveralNotNumbers_ShowsEveryText()
    {
        // Arrange
        var (viewModel, _) = Build(Defaults);
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.SetInput(PricingSettingsViewModel.Fields.Discount, "x");
        viewModel.SetInput(PricingSettingsViewModel.Fields.MaxChange, "y");

        // Act
        await viewModel.SaveAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["Enter a discount from 0 to 100 percent.", "Enter a largest change above 0 and at most 100 percent."], viewModel.Errors);
    }

    /// <summary>Negative: a refusal of the API shows the text of each code and keeps what was shown.</summary>
    [Fact]
    public async Task SaveAsync_RefusedByTheApi_ShowsTheTextsAndKeepsTheCurrentSettings()
    {
        // Arrange
        var (viewModel, service) = Build(Saved);
        service.SetAsync(Arg.Any<SetPricingSettingsRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<PricingSettingsDto>.FromCodes([PricingSettingErrors.ThresholdsOrderInvalid, PricingSettingErrors.DiscountSizesInvalid]));
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.SetDiscounted(RoomSize.Single, false);
        viewModel.SetDiscounted(RoomSize.Double, false);

        // Act
        await viewModel.SaveAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(
            ["The low season threshold must be below the high season threshold.", "Choose at least one room size for the discount."],
            viewModel.Errors);
        Assert.Equal(Saved, viewModel.Current);
        Assert.Null(viewModel.SuccessMessage);
        Assert.False(viewModel.IsBusy);
    }

    #endregion

    private static (PricingSettingsViewModel ViewModel, IPricingSettingsService Service) Build(PricingSettingsDto current)
    {
        var service = Substitute.For<IPricingSettingsService>();
        service.GetAsync(Arg.Any<CancellationToken>()).Returns(Result<PricingSettingsDto>.Success(current));
        return (new PricingSettingsViewModel(new PassThroughRunner<IPricingSettingsService>(service)), service);
    }
}
