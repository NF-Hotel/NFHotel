using Bunit;

using NFHotel.Application.Dto.Pricing;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Pricing;
using NFHotel.Web.Features.Admin.Pricing;
using NFHotel.Web.Security;

using NSubstitute;

using PricingSettingsPage = NFHotel.Web.Features.Admin.Pricing.PricingSettings;

namespace NFHotel.Web.Tests;

/// <summary>The pricing settings page of the Manager, rendered: the saved values and one discount check box per room size (MIL-055).</summary>
public sealed class PricingSettingsPageTests : IAsyncDisposable
{
    private readonly IPricingSettingsService _service = Substitute.For<IPricingSettingsService>();
    private readonly PageHost _host = new();

    public PricingSettingsPageTests()
    {
        _host.With(new PricingSettingsViewModel(new PassThroughRunner<IPricingSettingsService>(_service)));
        _host.SignedInWith(StaffPolicies.ManagePrices);
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    /// <summary>Positive: the saved settings fill the fields, only the room sizes with a discount are checked, and the page says who saved them.</summary>
    [Fact]
    public void Render_SavedSettings_ChecksOnlyTheDiscountedSizes()
    {
        // Arrange
        PricingSettingsDto saved = new(25m, 10m, 90m, 30m, 20m, 55.5m, ["Single", "Double"], "manager", new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero));
        _service.GetAsync(Arg.Any<CancellationToken>()).Returns(Result<PricingSettingsDto>.Success(saved));

        // Act
        IRenderedComponent<PricingSettingsPage> page = _host.Render<PricingSettingsPage>();

        // Assert
        Assert.True(page.Find("#setting-size-single").HasAttribute("checked"));
        Assert.True(page.Find("#setting-size-double").HasAttribute("checked"));
        Assert.False(page.Find("#setting-size-suite").HasAttribute("checked"));
        Assert.Equal("55.50", page.Find("#setting-minimum-price").GetAttribute("value"));
        Assert.Equal("Last saved by manager at 2026-11-01 10:00 UTC.", page.Find("#pricing-settings-saved").TextContent.Trim());
    }
}
