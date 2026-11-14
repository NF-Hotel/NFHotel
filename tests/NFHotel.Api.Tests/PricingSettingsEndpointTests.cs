using System.Net;
using System.Net.Http.Json;

using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Pricing;
using NFHotel.Application.Dto.Staff;
using NFHotel.Application.Errors;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Api.Tests;

/// <summary>The pricing settings endpoints (MIL-055): only a Manager reads or saves them, and the audit page shows the change.</summary>
public sealed class PricingSettingsEndpointTests
{
    private const string Route = "/api/pricing-settings";

    private static readonly SetPricingSettingsRequest Valid = new(25m, 15m, 80m, 40m, 30m, 0m, ["Single", "Double"]);

    #region Allowed calls

    /// <summary>Positive: a Manager reads the defaults before anything is saved.</summary>
    [Fact]
    public async Task Get_ManagerNothingSaved_ReturnsTheDefaults()
    {
        // Arrange
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateStaffClient(StaffRole.Manager);

        // Act
        ApiResponse<PricingSettingsDto>? body = await client.GetFromJsonAsync<ApiResponse<PricingSettingsDto>>(Route, ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((20m, 15m, 80m, 40m, 30m, 0m), (body!.Value!.SurchargePercent, body.Value.DiscountPercent, body.Value.HighThresholdPercent, body.Value.LowThresholdPercent, body.Value.MaxChangePercent, body.Value.MinimumNightlyPrice));
        Assert.Equal(["Single"], body.Value.DiscountSizes);
        Assert.Null(body.Value.SetBy);
        Assert.Null(body.Value.SetAt);
    }

    /// <summary>Positive: a Manager saves the settings, the answer carries them, the staff name comes from the token, and a read returns them.</summary>
    [Fact]
    public async Task Put_Manager_SavesTheSettingsAndNamesTheStaffMember()
    {
        // Arrange
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateStaffClient(StaffRole.Manager);

        // Act
        using HttpResponseMessage response = await client.PutAsJsonAsync(Route, Valid, ApiJson.Options, TestContext.Current.CancellationToken);
        ApiResponse<PricingSettingsDto>? body = await response.Content.ReadFromJsonAsync<ApiResponse<PricingSettingsDto>>(ApiJson.Options, TestContext.Current.CancellationToken);
        ApiResponse<PricingSettingsDto>? read = await client.GetFromJsonAsync<ApiResponse<PricingSettingsDto>>(Route, ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal((25m, "tester-manager"), (body!.Value!.SurchargePercent, body.Value.SetBy));
        Assert.Equal(["Single", "Double"], body.Value.DiscountSizes);
        Assert.Equal(["Single", "Double"], read!.Value!.DiscountSizes);
        Assert.NotNull(read.Value.SetAt);
        Assert.Single(factory.World.PricingSettings.Items);
    }

    /// <summary>Positive: the audit page returns the event with only the fields that changed.</summary>
    [Fact]
    public async Task Audit_AfterTwoSaves_ShowsOnlyTheChangedFields()
    {
        // Arrange
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateStaffClient(StaffRole.Manager);
        await client.PutAsJsonAsync(Route, Valid, ApiJson.Options, TestContext.Current.CancellationToken);
        await client.PutAsJsonAsync(Route, Valid with { DiscountPercent = 10m }, ApiJson.Options, TestContext.Current.CancellationToken);

        // Act
        ApiResponse<StaffAuditPageDto>? body = await client.GetFromJsonAsync<ApiResponse<StaffAuditPageDto>>("/api/staff/audit?page=1&pageSize=50", ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        StaffAuditEventDto[] events = body!.Value!.Items.Where(item => item.Action == StaffAuditAction.PricingSettingsChanged).ToArray();
        Assert.Equal(["Discount: 15.00 -> 10.00", "Surcharge: 20.00 -> 25.00; DiscountSizes: Single -> Single,Double"], events.Select(item => item.Detail));
        Assert.All(events, item => Assert.Equal("tester-manager", item.ActorName));
    }

    #endregion

    #region Refused calls

    /// <summary>Negative: bad values are refused with 422 and their codes, and nothing is stored.</summary>
    [Fact]
    public async Task Put_BadValues_Returns422WithTheCodes()
    {
        // Arrange
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateStaffClient(StaffRole.Manager);
        SetPricingSettingsRequest bad = new(101m, 15m, 40m, 40m, 0m, -1m, ["Penthouse"]);

        // Act
        using HttpResponseMessage response = await client.PutAsJsonAsync(Route, bad, ApiJson.Options, TestContext.Current.CancellationToken);
        ApiResponse<PricingSettingsDto>? body = await response.Content.ReadFromJsonAsync<ApiResponse<PricingSettingsDto>>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains(PricingSettingErrors.DiscountSizesInvalid, body!.Errors);
        Assert.Empty(factory.World.PricingSettings.Items);

        // Act
        using HttpResponseMessage second = await client.PutAsJsonAsync(Route, bad with { DiscountSizes = ["Single"] }, ApiJson.Options, TestContext.Current.CancellationToken);
        ApiResponse<PricingSettingsDto>? secondBody = await second.Content.ReadFromJsonAsync<ApiResponse<PricingSettingsDto>>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, second.StatusCode);
        Assert.Equivalent(
            new[]
            {
                PricingSettingErrors.SurchargeInvalid,
                PricingSettingErrors.ThresholdsOrderInvalid,
                PricingSettingErrors.MaxChangeInvalid,
                PricingSettingErrors.MinimumPriceInvalid,
            },
            secondBody!.Errors.ToArray());
        Assert.Empty(factory.World.StaffAudit.Events);
    }

    /// <summary>Negative (RC-266 N4): a first save that hits the unique index of the one row at the same moment as another is 409 with the conflict code.</summary>
    [Fact]
    public async Task Put_UniqueIndexBrokenBySimultaneousSave_Returns409WithTheConflictCode()
    {
        // Arrange
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateStaffClient(StaffRole.Manager);
        factory.World.UnitOfWork.FailWith = new UniqueConstraintViolationException("duplicate key", "ux_pricing_settings_slot");

        // Act
        using HttpResponseMessage response = await client.PutAsJsonAsync(Route, Valid, ApiJson.Options, TestContext.Current.CancellationToken);
        ApiResponse<PricingSettingsDto>? body = await response.Content.ReadFromJsonAsync<ApiResponse<PricingSettingsDto>>(ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains(PricingSettingErrors.ConcurrencyConflict, body!.Errors);
    }

    /// <summary>Negative: a role without the permission is 403 on the read and on the write, and nothing is stored.</summary>
    [Theory]
    [InlineData(StaffRole.Receptionist)]
    [InlineData(StaffRole.Housekeeping)]
    public async Task GetAndPut_RoleWithoutThePermission_Return403(StaffRole role)
    {
        // Arrange
        using ApiFactory factory = new();
        using HttpClient client = factory.CreateStaffClient(role);

        // Act
        using HttpResponseMessage get = await client.GetAsync(Route, TestContext.Current.CancellationToken);
        using HttpResponseMessage put = await client.PutAsJsonAsync(Route, Valid, ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, get.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, put.StatusCode);
        Assert.Empty(factory.World.PricingSettings.Items);
        Assert.Empty(factory.World.StaffAudit.Events);
    }

    /// <summary>Negative: the service token alone, or no token at all, is refused with 401.</summary>
    [Fact]
    public async Task GetAndPut_NoStaffToken_Return401()
    {
        // Arrange
        using ApiFactory factory = new();
        using HttpClient service = factory.CreateServiceClient();
        using HttpClient anonymous = factory.CreateClient();

        // Act
        using HttpResponseMessage serviceGet = await service.GetAsync(Route, TestContext.Current.CancellationToken);
        using HttpResponseMessage servicePut = await service.PutAsJsonAsync(Route, Valid, ApiJson.Options, TestContext.Current.CancellationToken);
        using HttpResponseMessage anonymousGet = await anonymous.GetAsync(Route, TestContext.Current.CancellationToken);
        using HttpResponseMessage anonymousPut = await anonymous.PutAsJsonAsync(Route, Valid, ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.All(new[] { serviceGet, servicePut, anonymousGet, anonymousPut }, response => Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));
        Assert.Empty(factory.World.PricingSettings.Items);
    }

    #endregion
}
