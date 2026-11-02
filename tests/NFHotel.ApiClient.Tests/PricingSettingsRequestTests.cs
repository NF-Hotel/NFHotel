using System.Net;

using NFHotel.Application.Dto.Pricing;
using NFHotel.Application.UseCases;

namespace NFHotel.ApiClient.Tests;

/// <summary>The web application reads and saves the pricing settings through the API (MIL-055).</summary>
public sealed class PricingSettingsRequestTests
{
    private static readonly Uri Base = new("http://api.test/");

    private const string SettingsJson =
        """{"value":{"surchargePercent":25.0,"discountPercent":15.0,"highThresholdPercent":80.0,"lowThresholdPercent":40.0,"maxChangePercent":30.0,"minimumNightlyPrice":0.0,"discountSizes":["Single","Double"],"setBy":"manager","setAt":"2026-11-01T10:00:00+00:00"},"errors":[]}""";

    /// <summary>Positive: reading is a GET of the pricing settings path, and the answer is read.</summary>
    [Fact]
    public async Task GetAsync_Call_GetsThePricingSettingsPath()
    {
        // Arrange
        (StubHandler? handler, ApiPricingSettingsService? service) = Build(SettingsJson);

        // Act
        Result<PricingSettingsDto> result = await service.GetAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(["Single", "Double"], result.Value.DiscountSizes);
        Assert.Equal((HttpMethod.Get, "/api/pricing-settings"), (handler.Method!, handler.PathAndQuery!));
    }

    /// <summary>Positive: saving is a PUT of the same path.</summary>
    [Fact]
    public async Task SetAsync_Request_PutsThePricingSettingsPath()
    {
        // Arrange
        (StubHandler? handler, ApiPricingSettingsService? service) = Build(SettingsJson);

        // Act
        Result<PricingSettingsDto> result = await service.SetAsync(new SetPricingSettingsRequest(25m, 15m, 80m, 40m, 30m, 0m, ["Single", "Double"]), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(25m, result.Value.SurchargePercent);
        Assert.Equal((HttpMethod.Put, "/api/pricing-settings"), (handler.Method!, handler.PathAndQuery!));
    }

    /// <summary>Negative: a refusal of the API comes back as its codes.</summary>
    [Fact]
    public async Task SetAsync_Refused_ReturnsTheCodes()
    {
        // Arrange
        StubHandler handler = StubHandler.Json(HttpStatusCode.UnprocessableEntity, """{"value":null,"errors":["pricing_setting.surcharge_invalid"]}""");
        ApiGateway gateway = new(new HttpClient(handler, disposeHandler: false) { BaseAddress = Base });

        // Act
        Result<PricingSettingsDto> result = await new ApiPricingSettingsService(gateway).SetAsync(new SetPricingSettingsRequest(101m, 15m, 80m, 40m, 30m, 0m, ["Single"]), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("pricing_setting.surcharge_invalid", result.Errors.Select(error => error.Code));
    }

    private static (StubHandler Handler, ApiPricingSettingsService Service) Build(string json)
    {
        StubHandler handler = StubHandler.Json(HttpStatusCode.OK, json);
        ApiGateway gateway = new(new HttpClient(handler, disposeHandler: false) { BaseAddress = Base });
        return (handler, new ApiPricingSettingsService(gateway));
    }
}
