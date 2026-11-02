using NFHotel.Application.Dto.Pricing;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Pricing;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

namespace NFHotel.Application.Tests.Services;

/// <summary>Reading and saving the pricing settings of the Manager, with their audit event (MIL-055).</summary>
public sealed class PricingSettingsServiceTests
{
    private static readonly SetPricingSettingsRequest DefaultsRequest = new(20m, 15m, 80m, 40m, 30m, 0m, ["Single"]);

    public static TheoryData<string[]> BadSizes => new()
    {
        Array.Empty<string>(),
        new[] { "Penthouse" },
        new[] { "Single", "1" },
        new[] { string.Empty },
        null!,
    };

    private static ServiceWorld WorldWithManager()
    {
        ServiceWorld world = new();
        world.CallContext.StaffId = 7;
        world.CallContext.StaffName = "manager";
        world.CallContext.RemoteAddress = "10.0.0.5";
        return world;
    }

    #region Reading

    /// <summary>Positive: with nothing saved the defaults come back without a saver and a time.</summary>
    [Fact]
    public async Task GetAsync_NothingSaved_ReturnsTheDefaultsWithoutSaverAndTime()
    {
        // Arrange
        ServiceWorld world = WorldWithManager();

        // Act
        Result<PricingSettingsDto> result = await world.PricingSettingsService().GetAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        PricingSettingsDto value = result.Value;
        Assert.Equal((20m, 15m, 80m, 40m, 30m, 0m), (value.SurchargePercent, value.DiscountPercent, value.HighThresholdPercent, value.LowThresholdPercent, value.MaxChangePercent, value.MinimumNightlyPrice));
        Assert.Equal(["Single"], value.DiscountSizes);
        Assert.Null(value.SetBy);
        Assert.Null(value.SetAt);
        Assert.Empty(world.PricingSettings.Items);
    }

    /// <summary>Positive: saved settings come back with the saver and the clock time.</summary>
    [Fact]
    public async Task GetAsync_SettingsSaved_ReturnsThemWithSaverAndTime()
    {
        // Arrange
        ServiceWorld world = WorldWithManager();
        PricingSettingsService service = world.PricingSettingsService();
        await service.SetAsync(DefaultsRequest with { SurchargePercent = 25m, DiscountSizes = ["double", "Single"] }, TestContext.Current.CancellationToken);

        // Act
        Result<PricingSettingsDto> result = await service.GetAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(25m, result.Value.SurchargePercent);
        Assert.Equal(["Single", "Double"], result.Value.DiscountSizes);
        Assert.Equal(("manager", (DateTimeOffset?)world.Clock.UtcNow), (result.Value.SetBy, result.Value.SetAt));
    }

    #endregion

    #region Saving and the audit trail

    /// <summary>Positive: the first save is compared with the defaults, stored once, and audited with only the changed field.</summary>
    [Fact]
    public async Task SetAsync_FirstSave_ComparesWithTheDefaultsAndAuditsOnlyWhatChanged()
    {
        // Arrange
        ServiceWorld world = WorldWithManager();

        // Act
        Result<PricingSettingsDto> result = await world.PricingSettingsService().SetAsync(DefaultsRequest with { SurchargePercent = 25m }, TestContext.Current.CancellationToken);

        // Assert
        PricingSetting stored = Assert.Single(world.PricingSettings.Items);
        StaffAuditEvent audit = Assert.Single(world.StaffAudit.Events);
        Assert.True(result.IsSuccess);
        Assert.Equal((25m, "manager"), (stored.SurchargePercent, stored.SetBy));
        Assert.Equal(world.Clock.UtcNow, stored.SetAt);
        Assert.Equal(StaffAuditAction.PricingSettingsChanged, audit.Action);
        Assert.Equal("Surcharge: 20.00 -> 25.00", audit.Detail);
        Assert.Equal((7, "manager", "Success"), (audit.ActorId, audit.ActorName, audit.Outcome));
        Assert.Null(audit.TargetId);
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>Positive: a later save replaces the row, adds no second one and lists only the fields that changed, sizes included.</summary>
    [Fact]
    public async Task SetAsync_SecondSave_ReplacesTheRowAndListsOnlyTheChangedFields()
    {
        // Arrange
        ServiceWorld world = WorldWithManager();
        PricingSettingsService service = world.PricingSettingsService();
        await service.SetAsync(DefaultsRequest with { SurchargePercent = 25m }, TestContext.Current.CancellationToken);
        world.CallContext.StaffName = "other";

        // Act
        Result<PricingSettingsDto> result = await service.SetAsync(
            DefaultsRequest with { SurchargePercent = 25m, DiscountPercent = 10.5m, MinimumNightlyPrice = 60m, DiscountSizes = ["Single", "Double"] },
            TestContext.Current.CancellationToken);

        // Assert
        PricingSetting stored = Assert.Single(world.PricingSettings.Items);
        Assert.Equal(("other", 10.5m), (stored.SetBy, stored.DiscountPercent));
        Assert.Equal("other", result.Value.SetBy);
        Assert.Equal(
            ["Surcharge: 20.00 -> 25.00", "Discount: 15.00 -> 10.50; MinimumPrice: 0.00 -> 60.00; DiscountSizes: Single -> Single,Double"],
            world.StaffAudit.Events.Select(item => item.Detail));
        Assert.Equal(2, world.UnitOfWork.SaveCount);
    }

    /// <summary>Equivalence: every field is named in the audit text with its old and new value.</summary>
    [Fact]
    public async Task SetAsync_EveryFieldChanged_NamesEachOfThemInOrder()
    {
        // Arrange
        ServiceWorld world = WorldWithManager();

        // Act
        await world.PricingSettingsService().SetAsync(new SetPricingSettingsRequest(21m, 16m, 90m, 30m, 40m, 5m, ["Suite"]), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(
            "Surcharge: 20.00 -> 21.00; Discount: 15.00 -> 16.00; HighThreshold: 80.00 -> 90.00; LowThreshold: 40.00 -> 30.00; "
            + "MaxChange: 30.00 -> 40.00; MinimumPrice: 0.00 -> 5.00; DiscountSizes: Single -> Suite",
            Assert.Single(world.StaffAudit.Events).Detail);
    }

    /// <summary>Boundary: the audit text of a save that changes every field never exceeds 200 characters.</summary>
    [Fact]
    public async Task SetAsync_LargestAuditText_NeverExceeds200Characters()
    {
        // Arrange
        ServiceWorld world = WorldWithManager();
        PricingSettingsService service = world.PricingSettingsService();
        await service.SetAsync(new SetPricingSettingsRequest(100m, 100m, 99.99m, 0.01m, 100m, 999999.99m, ["Single", "Double", "Suite"]), TestContext.Current.CancellationToken);

        // Act
        await service.SetAsync(new SetPricingSettingsRequest(0m, 0m, 70m, 60m, 0.01m, 0m, ["Suite"]), TestContext.Current.CancellationToken);

        // Assert
        Assert.All(world.StaffAudit.Events, item => Assert.True(item.Detail!.Length <= StaffAuditEvent.MaxDetailLength, item.Detail));
        Assert.Equal(2, world.StaffAudit.Events.Count);
    }

    /// <summary>State: sizes in another order or with a repeat are the same set, so nothing changes.</summary>
    [Fact]
    public async Task SetAsync_SameSizesInAnotherOrder_SavesNothing()
    {
        // Arrange
        ServiceWorld world = WorldWithManager();
        PricingSettingsService service = world.PricingSettingsService();
        await service.SetAsync(DefaultsRequest with { DiscountSizes = ["Single", "Suite"] }, TestContext.Current.CancellationToken);
        int saves = world.UnitOfWork.SaveCount;

        // Act
        await service.SetAsync(DefaultsRequest with { DiscountSizes = ["suite", "SINGLE", "Suite"] }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(world.StaffAudit.Events);
        Assert.Equal(saves, world.UnitOfWork.SaveCount);
    }

    /// <summary>State: saving what is already saved stores nothing, writes no audit event and returns the current values.</summary>
    [Fact]
    public async Task SetAsync_NothingChanged_SavesNothingAndAuditsNothing()
    {
        // Arrange
        ServiceWorld world = WorldWithManager();
        PricingSettingsService service = world.PricingSettingsService();
        await service.SetAsync(DefaultsRequest with { SurchargePercent = 25m }, TestContext.Current.CancellationToken);
        int saves = world.UnitOfWork.SaveCount;
        DateTimeOffset firstSetAt = world.PricingSettings.Items[0].SetAt;
        world.Clock.TimeOfDay = TimeSpan.FromHours(13);
        world.CallContext.StaffName = "other";

        // Act
        Result<PricingSettingsDto> result = await service.SetAsync(DefaultsRequest with { SurchargePercent = 25.001m }, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(("manager", (DateTimeOffset?)firstSetAt), (result.Value.SetBy, result.Value.SetAt));
        Assert.Equal(25m, result.Value.SurchargePercent);
        Assert.Single(world.StaffAudit.Events);
        Assert.Equal(saves, world.UnitOfWork.SaveCount);
        Assert.Equal("manager", world.PricingSettings.Items[0].SetBy);
    }

    /// <summary>State: saving the defaults when nothing is stored stores nothing and returns the defaults.</summary>
    [Fact]
    public async Task SetAsync_DefaultsWhileNothingStored_SavesNothing()
    {
        // Arrange
        ServiceWorld world = WorldWithManager();

        // Act
        Result<PricingSettingsDto> result = await world.PricingSettingsService().SetAsync(DefaultsRequest, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.SetAt);
        Assert.Empty(world.PricingSettings.Items);
        Assert.Empty(world.StaffAudit.Events);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    #endregion

    #region Refused saves

    /// <summary>Negative: an invalid value is refused with its code and nothing is stored, audited or saved.</summary>
    [Fact]
    public async Task SetAsync_InvalidValue_ReturnsTheCodeAndSavesNothing()
    {
        // Arrange
        ServiceWorld world = WorldWithManager();

        // Act
        Result<PricingSettingsDto> result = await world.PricingSettingsService().SetAsync(DefaultsRequest with { SurchargePercent = 150m }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([PricingSettingErrors.SurchargeInvalid], result.Errors.Select(error => error.Code));
        Assert.Empty(world.PricingSettings.Items);
        Assert.Empty(world.StaffAudit.Events);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: an invalid value keeps the settings that are saved and writes no second audit event.</summary>
    [Fact]
    public async Task SetAsync_InvalidValueAfterASave_KeepsTheSavedSettings()
    {
        // Arrange
        ServiceWorld world = WorldWithManager();
        PricingSettingsService service = world.PricingSettingsService();
        await service.SetAsync(DefaultsRequest with { SurchargePercent = 25m }, TestContext.Current.CancellationToken);
        int saves = world.UnitOfWork.SaveCount;

        // Act
        Result<PricingSettingsDto> result = await service.SetAsync(DefaultsRequest with { SurchargePercent = 30m, LowThresholdPercent = 90m }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains(PricingSettingErrors.ThresholdsOrderInvalid, result.Errors.Select(error => error.Code));
        Assert.Equal(25m, Assert.Single(world.PricingSettings.Items).SurchargePercent);
        Assert.Single(world.StaffAudit.Events);
        Assert.Equal(saves, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: several faults come back together.</summary>
    [Fact]
    public async Task SetAsync_SeveralFaults_ReturnsEveryCode()
    {
        // Arrange
        ServiceWorld world = WorldWithManager();

        // Act
        Result<PricingSettingsDto> result = await world.PricingSettingsService().SetAsync(new SetPricingSettingsRequest(-1m, 15m, 80m, 40m, 0m, 0m, ["Single"]), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equivalent(
            new[] { PricingSettingErrors.SurchargeInvalid, PricingSettingErrors.MaxChangeInvalid },
            result.Errors.Select(error => error.Code).ToArray());
    }

    /// <summary>Negative: no sizes, or a name that is not a size, is refused with the sizes code.</summary>
    [Theory]
    [MemberData(nameof(BadSizes))]
    public async Task SetAsync_BadSizes_ReturnsTheSizesCodeAndSavesNothing(string[] sizes)
    {
        // Arrange
        ServiceWorld world = WorldWithManager();

        // Act
        Result<PricingSettingsDto> result = await world.PricingSettingsService().SetAsync(DefaultsRequest with { DiscountSizes = sizes }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([PricingSettingErrors.DiscountSizesInvalid], result.Errors.Select(error => error.Code));
        Assert.Empty(world.PricingSettings.Items);
        Assert.Empty(world.StaffAudit.Events);
    }

    /// <summary>Negative: a call with nobody signed in is refused with the set-by code, and nothing is written.</summary>
    [Fact]
    public async Task SetAsync_NobodySignedIn_ReturnsTheSetByCodeAndWritesNothing()
    {
        // Arrange
        ServiceWorld world = new();

        // Act
        Result<PricingSettingsDto> result = await world.PricingSettingsService().SetAsync(DefaultsRequest with { SurchargePercent = 25m }, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([PricingSettingErrors.SetByInvalid], result.Errors.Select(error => error.Code));
        Assert.Empty(world.PricingSettings.Items);
        Assert.Empty(world.StaffAudit.Events);
    }

    /// <summary>Negative (RC-265 F13): a simultaneous first save hits the unique index; the call returns the conflict code, not an exception.</summary>
    [Fact]
    public async Task SetAsync_UniqueIndexBrokenBySimultaneousFirstSave_ReturnsTheConflictCode()
    {
        // Arrange
        ServiceWorld world = WorldWithManager();
        world.UnitOfWork.FailWith = new UniqueConstraintViolationException("duplicate key", "ux_pricing_settings_slot");

        // Act
        Result<PricingSettingsDto> result = await world.PricingSettingsService().SetAsync(DefaultsRequest with { SurchargePercent = 25m }, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(PricingSettingErrors.ConcurrencyConflict, result.FirstError!.Code);
    }

    #endregion

    #region The mapping to the rule

    /// <summary>Equivalence: every combination of the bounds that the entity accepts is accepted by the rule too.</summary>
    [Theory]
    [InlineData("0", "0", "100", "0", "0.01", "0")]
    [InlineData("100", "100", "0.01", "0", "100", "1000000")]
    [InlineData("50", "50", "99.99", "99.98", "50", "500")]
    public void ToRule_AnythingTheEntityAccepts_PassesEnsureValid(string surcharge, string discount, string high, string low, string maxChange, string minimum)
    {
        // Arrange
        decimal Parse(string text) => decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
        PricingSetting setting = PricingSetting.Create(Parse(surcharge), Parse(discount), Parse(high), Parse(low), Parse(maxChange), Parse(minimum), [RoomSize.Suite, RoomSize.Single], "manager", DateTimeOffset.UnixEpoch);

        // Act
        PricingSettings rule = setting.ToRule();
        Action check = rule.EnsureValid;

        // Assert
        Assert.Null(Record.Exception(check));
        Assert.Equal((Parse(high), Parse(low)), (rule.HighThreshold, rule.LowThreshold));
        Assert.Equal([RoomSize.Single, RoomSize.Suite], rule.DiscountSizes);
    }

    /// <summary>State: the defaults of the service are the defaults of the rule.</summary>
    [Fact]
    public async Task GetAsync_NothingSaved_MatchesTheDefaultsOfTheRule()
    {
        // Arrange
        ServiceWorld world = WorldWithManager();
        PricingSettings defaults = PricingSettings.Defaults;

        // Act
        PricingSettingsDto value = (await world.PricingSettingsService().GetAsync(TestContext.Current.CancellationToken)).Value;

        // Assert
        Assert.Equal(
            (defaults.SurchargePercent, defaults.DiscountPercent, defaults.HighThreshold, defaults.LowThreshold, defaults.MaxChangePercent, defaults.MinimumNightlyPrice),
            (value.SurchargePercent, value.DiscountPercent, value.HighThresholdPercent, value.LowThresholdPercent, value.MaxChangePercent, value.MinimumNightlyPrice));
        Assert.Equal(defaults.DiscountSizes.Select(size => size.ToString()), value.DiscountSizes);
    }

    #endregion
}
