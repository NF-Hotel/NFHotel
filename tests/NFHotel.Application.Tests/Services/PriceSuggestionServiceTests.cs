using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Dto.Pricing;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

namespace NFHotel.Application.Tests.Services;

/// <summary>The open price suggestions, their approval and the revert, with the audit events (MIL-055, UC-007).</summary>
public sealed class PriceSuggestionServiceTests
{
    private const string HighSingleKey = "single|2027-06-01|2027-08-31|20.00";
    private const string LowSingleKey = "single|2028-01-01|2028-02-29|-15.00";

    private static readonly DateOnly SummerFirst = new(2027, 6, 1);
    private static readonly DateOnly SummerLast = new(2027, 8, 31);

    /// <summary>Today is 2027-05-01, so the horizon is June 2027 to May 2028; rates are 100, 150 and 200; the report is seasonal.</summary>
    private static ServiceWorld WorldWithReport(MarketAnalysisContentDto? content = null, bool allRates = true)
    {
        var world = new ServiceWorld();
        world.CallContext.StaffId = 7;
        world.CallContext.StaffName = "manager";
        world.Clock.Today = new DateOnly(2027, 5, 1);
        world.RoomRates.Add(RoomRate.Create(RoomSize.Single, 100m, "manager", world.Clock.UtcNow));
        if (allRates)
        {
            world.RoomRates.Add(RoomRate.Create(RoomSize.Double, 150m, "manager", world.Clock.UtcNow));
            world.RoomRates.Add(RoomRate.Create(RoomSize.Suite, 200m, "manager", world.Clock.UtcNow));
        }

        PriceSuggestionTestData.StoreLatestReport(world, content ?? PriceSuggestionTestData.Content());
        return world;
    }

    private static async Task<PriceSuggestionListDto> OpenAsync(ServiceWorld world)
    {
        var result = await world.PriceSuggestionService().GetOpenAsync(TestContext.Current.CancellationToken);
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    #region Open suggestions

    /// <summary>Positive: a high summer gives +20 % on all three sizes with the old and the new price, and a low winter gives -15 % on the single room only.</summary>
    [Fact]
    public async Task GetOpenAsync_SeasonalReport_ReturnsTheHandFigures()
    {
        // Arrange
        var world = WorldWithReport();

        // Act
        var list = await OpenAsync(world);

        // Assert
        Assert.Equal(PriceSuggestionBasis.Ok, list.Basis);
        Assert.Equal(new DateOnly(2027, 5, 1), list.GeneratedForDay);
        Assert.Equal(
            [
                new PriceSuggestionItemDto(HighSingleKey, "Single", "High", SummerFirst, SummerLast, 20m, 100m, 120m, 90m, 80m, false),
                new PriceSuggestionItemDto("double|2027-06-01|2027-08-31|20.00", "Double", "High", SummerFirst, SummerLast, 20m, 150m, 180m, 90m, 80m, false),
                new PriceSuggestionItemDto("suite|2027-06-01|2027-08-31|20.00", "Suite", "High", SummerFirst, SummerLast, 20m, 200m, 240m, 90m, 80m, false),
                new PriceSuggestionItemDto(LowSingleKey, "Single", "Low", new DateOnly(2028, 1, 1), new DateOnly(2028, 2, 29), -15m, 100m, 85m, 30m, 40m, false),
            ],
            list.Items);
        Assert.Null(list.AiExplanation);
    }

    /// <summary>Positive: a size without a base rate gets no suggestion.</summary>
    [Fact]
    public async Task GetOpenAsync_OnlyTheSingleRateIsSet_SuggestsForTheSingleRoomOnly()
    {
        // Arrange
        var world = WorldWithReport(allRates: false);

        // Act
        var list = await OpenAsync(world);

        // Assert
        Assert.Equal([HighSingleKey, LowSingleKey], list.Items.Select(item => item.Key));
    }

    /// <summary>Positive: saved settings are used instead of the defaults (a surcharge of 25 % gives 125).</summary>
    [Fact]
    public async Task GetOpenAsync_SettingsSaved_UsesThem()
    {
        // Arrange
        var world = WorldWithReport();
        world.PricingSettings.Add(PricingSetting.Create(25m, 15m, 80m, 40m, 30m, 0m, [RoomSize.Single], "manager", world.Clock.UtcNow));

        // Act
        var list = await OpenAsync(world);

        // Assert
        var single = list.Items.First(item => item.Size == "Single" && item.Season == "High");
        Assert.Equal(("single|2027-06-01|2027-08-31|25.00", 25m, 125m), (single.Key, single.Percentage, single.NewPrice));
    }

    /// <summary>Positive: a surcharge above the largest change is cut and flagged as limited.</summary>
    [Fact]
    public async Task GetOpenAsync_SurchargeAboveTheLargestChange_IsLimited()
    {
        // Arrange
        var world = WorldWithReport();
        world.PricingSettings.Add(PricingSetting.Create(40m, 15m, 80m, 40m, 30m, 0m, [RoomSize.Single], "manager", world.Clock.UtcNow));

        // Act
        var list = await OpenAsync(world);

        // Assert
        var single = list.Items.First(item => item.Size == "Single" && item.Season == "High");
        Assert.Equal((30m, 130m, true), (single.Percentage, single.NewPrice, single.WasLimited));
    }

    /// <summary>Negative: without any stored report the basis is no_basis and the list is empty (extension 2a).</summary>
    [Fact]
    public async Task GetOpenAsync_NoReport_ReturnsNoBasis()
    {
        // Arrange
        var world = new ServiceWorld();
        world.Clock.Today = new DateOnly(2027, 5, 1);
        world.RoomRates.Add(RoomRate.Create(RoomSize.Single, 100m, "manager", world.Clock.UtcNow));

        // Act
        var list = await OpenAsync(world);

        // Assert
        Assert.Equal(PriceSuggestionBasis.NoBasis, list.Basis);
        Assert.Empty(list.Items);
        Assert.Null(list.AiExplanation);
    }

    /// <summary>Negative: a report without a usable occupancy gives no_basis (an unavailable section, another section, fewer than twelve months).</summary>
    [Fact]
    public async Task GetOpenAsync_ReportWithoutUsableOccupancy_ReturnsNoBasis()
    {
        // Arrange
        var good = PriceSuggestionTestData.Content(explanation: "Text.");
        var unavailable = new MarketAnalysisContentDto([], [good.Sections[0] with { Status = "unavailable" }]);
        var other = new MarketAnalysisContentDto([], [good.Sections[0] with { Id = "lead_time" }]);
        var elevenMonths = new MarketAnalysisContentDto(
            [],
            [good.Sections[0] with { Series = [good.Sections[0].Series[0] with { Points = [.. good.Sections[0].Series[0].Points.Take(11)] }] }]);

        foreach (var world in new[] { unavailable, other, elevenMonths }.Select(content => WorldWithReport(content)))
        {
            // Act
            var list = await OpenAsync(world);

            // Assert
            Assert.Equal(PriceSuggestionBasis.NoBasis, list.Basis);
            Assert.Empty(list.Items);
            Assert.Null(list.AiExplanation);
        }
    }

    /// <summary>Positive: a year with no high and no low month has a basis but no suggestions.</summary>
    [Fact]
    public async Task GetOpenAsync_OnlyNormalMonths_ReturnsAnOkListWithoutItems()
    {
        // Arrange
        var world = WorldWithReport(PriceSuggestionTestData.Content(Enumerable.Repeat(60d, 12).ToArray()));

        // Act
        var list = await OpenAsync(world);

        // Assert
        Assert.Equal(PriceSuggestionBasis.Ok, list.Basis);
        Assert.Empty(list.Items);
    }

    /// <summary>Positive: the text of the AI insight is passed on, and changing it changes no figure (criterion 3).</summary>
    [Fact]
    public async Task GetOpenAsync_ExplanationText_IsPassedOnAndNeverChangesTheNumbers()
    {
        // Arrange
        var first = WorldWithReport(PriceSuggestionTestData.Content(explanation: "Raise every price by 99 percent in July."));
        var second = WorldWithReport(PriceSuggestionTestData.Content(explanation: "Lower everything to 1 krone."));
        var none = WorldWithReport(PriceSuggestionTestData.Content());

        // Act
        var withFirst = await OpenAsync(first);
        var withSecond = await OpenAsync(second);
        var withNone = await OpenAsync(none);

        // Assert
        Assert.Equal("Raise every price by 99 percent in July.", withFirst.AiExplanation);
        Assert.Equal("Lower everything to 1 krone.", withSecond.AiExplanation);
        Assert.Null(withNone.AiExplanation);
        Assert.Equal(withNone.Items, withFirst.Items);
        Assert.Equal(withNone.Items, withSecond.Items);
    }

    #endregion

    #region Suggestions that a change already covers

    /// <summary>Positive: after an approval the suggestion leaves the list; the other sizes stay.</summary>
    [Fact]
    public async Task GetOpenAsync_AfterApproval_HidesTheCoveredSuggestion()
    {
        // Arrange
        var world = WorldWithReport();
        var service = world.PriceSuggestionService();
        await service.ApproveAsync(new PriceSuggestionApproveRequest(HighSingleKey), TestContext.Current.CancellationToken);

        // Act
        var list = await OpenAsync(world);

        // Assert
        Assert.DoesNotContain(list.Items, item => item.Key == HighSingleKey);
        Assert.Equal(3, list.Items.Count);
    }

    /// <summary>Positive: two changes that together cover the whole period hide the suggestion.</summary>
    [Fact]
    public async Task GetOpenAsync_TwoChangesCoverThePeriod_HidesIt()
    {
        // Arrange
        var world = WorldWithReport();
        var service = world.PriceSuggestionService();
        await service.ApproveAsync(new PriceSuggestionApproveRequest(HighSingleKey, SummerFirst, new DateOnly(2027, 6, 30)), TestContext.Current.CancellationToken);
        await service.ApproveAsync(new PriceSuggestionApproveRequest(HighSingleKey, new DateOnly(2027, 7, 1), SummerLast), TestContext.Current.CancellationToken);

        // Act
        var list = await OpenAsync(world);

        // Assert
        Assert.DoesNotContain(list.Items, item => item.Key == HighSingleKey);
    }

    /// <summary>Negative: a change that covers only a part of the period leaves the suggestion in the list.</summary>
    [Fact]
    public async Task GetOpenAsync_PartlyCovered_KeepsTheSuggestion()
    {
        // Arrange
        var world = WorldWithReport();
        await world.PriceSuggestionService().ApproveAsync(new PriceSuggestionApproveRequest(HighSingleKey, SummerFirst, new DateOnly(2027, 6, 30)), TestContext.Current.CancellationToken);

        // Act
        var list = await OpenAsync(world);

        // Assert
        Assert.Contains(list.Items, item => item.Key == HighSingleKey);
    }

    /// <summary>Negative: a change of another size, and a reverted change, hide nothing.</summary>
    [Fact]
    public async Task GetOpenAsync_ChangeOfAnotherSizeOrRevertedChange_HidesNothing()
    {
        // Arrange
        var world = WorldWithReport();
        var service = world.PriceSuggestionService();
        await service.ApproveAsync(new PriceSuggestionApproveRequest("double|2027-06-01|2027-08-31|20.00"), TestContext.Current.CancellationToken);
        var single = await service.ApproveAsync(new PriceSuggestionApproveRequest(HighSingleKey), TestContext.Current.CancellationToken);
        await service.RevertAsync(single.Value.Change.Id, TestContext.Current.CancellationToken);

        // Act
        var list = await OpenAsync(world);

        // Assert
        Assert.Contains(list.Items, item => item.Key == HighSingleKey);
        Assert.DoesNotContain(list.Items, item => item.Key.StartsWith("double", StringComparison.Ordinal));
    }

    #endregion

    #region Approving

    /// <summary>Positive: a key from the list creates one change with the percentage of the suggestion, the audit event with the detail and the actor, in one save.</summary>
    [Fact]
    public async Task ApproveAsync_KeyFromTheList_CreatesTheChangeAndOneAuditEvent()
    {
        // Arrange
        var world = WorldWithReport();
        var key = (await OpenAsync(world)).Items[0].Key;

        // Act
        var result = await world.PriceSuggestionService().ApproveAsync(new PriceSuggestionApproveRequest(key), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Created);
        var stored = Assert.Single(world.PriceChanges.Items);
        Assert.Equal((RoomSize.Single, 20m, SummerFirst, SummerLast), (stored.Size, stored.Percentage, stored.FirstDay, stored.LastDay));
        var audit = Assert.Single(world.StaffAudit.Events);
        Assert.Equal(StaffAuditAction.PriceChangeApproved, audit.Action);
        Assert.Equal("Single +20.00% 2027-06-01..2027-08-31 (100.00 -> 120.00)", audit.Detail);
        Assert.Equal(("manager", "Success"), (audit.ActorName, audit.Outcome));
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>Positive: a discount is approved with its negative percentage and the detail shows the sign and the new price.</summary>
    [Fact]
    public async Task ApproveAsync_DiscountKey_StoresTheNegativePercentage()
    {
        // Arrange
        var world = WorldWithReport();

        // Act
        var result = await world.PriceSuggestionService().ApproveAsync(new PriceSuggestionApproveRequest(LowSingleKey), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(-15m, Assert.Single(world.PriceChanges.Items).Percentage);
        Assert.Equal("Single -15.00% 2028-01-01..2028-02-29 (100.00 -> 85.00)", Assert.Single(world.StaffAudit.Events).Detail);
    }

    /// <summary>
    /// Regression (RC-265 F1): with a minimum of 90 and a base rate of 100 the low season suggestion says 90.00; approving it
    /// gives an effective price of at least 90, the one the page announced, not 85.
    /// </summary>
    [Fact]
    public async Task ApproveAsync_DiscountCutByTheMinimumPrice_EffectivePriceIsTheSuggestedPriceAndNotBelowTheMinimum()
    {
        // Arrange
        var world = WorldWithReport();
        world.PricingSettings.Add(PricingSetting.Create(20m, 15m, 80m, 40m, 30m, 90m, [RoomSize.Single], "manager", world.Clock.UtcNow));
        var service = world.PriceSuggestionService();
        var item = (await OpenAsync(world)).Items.Single(candidate => candidate.Season == "Low");

        // Act
        var approved = await service.ApproveAsync(new PriceSuggestionApproveRequest(item.Key), TestContext.Current.CancellationToken);
        var effective = await world.PriceChangeService().GetEffectivePriceAsync("Single", new DateOnly(2028, 1, 15), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(approved.IsSuccess);
        Assert.Equal(90.00m, item.NewPrice);
        Assert.Equal(-10m, item.Percentage);
        Assert.True(item.WasLimited);
        Assert.Equal(item.NewPrice, effective.Value.Price);
        Assert.True(effective.Value.Price >= 90m);
    }

    /// <summary>Equivalence: the Manager may change the dates; the detail shows the dates that were approved, and one date alone keeps the other of the suggestion.</summary>
    [Fact]
    public async Task ApproveAsync_ChangedDates_AreUsed()
    {
        // Arrange
        var world = WorldWithReport();
        var service = world.PriceSuggestionService();

        // Act
        var both = await service.ApproveAsync(new PriceSuggestionApproveRequest(HighSingleKey, new DateOnly(2027, 6, 15), new DateOnly(2027, 7, 15)), TestContext.Current.CancellationToken);
        var onlyLast = await service.ApproveAsync(new PriceSuggestionApproveRequest("double|2027-06-01|2027-08-31|20.00", LastDay: new DateOnly(2027, 7, 31)), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((new DateOnly(2027, 6, 15), new DateOnly(2027, 7, 15)), (both.Value.Change.FirstDay, both.Value.Change.LastDay));
        Assert.Equal((SummerFirst, new DateOnly(2027, 7, 31)), (onlyLast.Value.Change.FirstDay, onlyLast.Value.Change.LastDay));
        Assert.Equal("Single +20.00% 2027-06-15..2027-07-15 (100.00 -> 120.00)", world.StaffAudit.Events[0].Detail);
    }

    /// <summary>Positive: the same approval twice creates one change and one audit event, and the second answer says it was not created.</summary>
    [Fact]
    public async Task ApproveAsync_Repeated_CreatesNothingAndNoSecondAuditEvent()
    {
        // Arrange
        var world = WorldWithReport();
        var service = world.PriceSuggestionService();
        var first = await service.ApproveAsync(new PriceSuggestionApproveRequest(HighSingleKey), TestContext.Current.CancellationToken);

        // Act
        var second = await service.ApproveAsync(new PriceSuggestionApproveRequest(HighSingleKey), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(second.IsSuccess);
        Assert.False(second.Value.Created);
        Assert.Equal(first.Value.Change.Id, second.Value.Change.Id);
        Assert.Single(world.PriceChanges.Items);
        Assert.Single(world.StaffAudit.Events);
        Assert.Equal(1, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: an unknown, empty or doctored key (a percentage the server did not compute) is refused with not found and nothing is saved.</summary>
    [Theory]
    [InlineData("nonsense")]
    [InlineData("")]
    [InlineData("single|2027-06-01|2027-08-31|99.00")]
    [InlineData("SINGLE|2027-06-01|2027-08-31|20.00")]
    [InlineData(null)]
    public async Task ApproveAsync_UnknownKey_ReturnsNotFoundAndSavesNothing(string? key)
    {
        // Arrange
        var world = WorldWithReport();

        // Act
        var result = await world.PriceSuggestionService().ApproveAsync(new PriceSuggestionApproveRequest(key!), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(PriceSuggestionErrors.NotFound, result.FirstError!.Code);
        Assert.Empty(world.PriceChanges.Items);
        Assert.Empty(world.StaffAudit.Events);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: when the report has gone, a key that was valid is refused with not found.</summary>
    [Fact]
    public async Task ApproveAsync_NoBasisAnyMore_ReturnsNotFound()
    {
        // Arrange
        var world = new ServiceWorld();
        world.CallContext.StaffName = "manager";
        world.Clock.Today = new DateOnly(2027, 5, 1);
        world.RoomRates.Add(RoomRate.Create(RoomSize.Single, 100m, "manager", world.Clock.UtcNow));

        // Act
        var result = await world.PriceSuggestionService().ApproveAsync(new PriceSuggestionApproveRequest(HighSingleKey), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(PriceSuggestionErrors.NotFound, result.FirstError!.Code);
        Assert.Empty(world.StaffAudit.Events);
    }

    /// <summary>Negative: dates that overlap a change of the same size are refused with the overlap code, with no audit event and no save.</summary>
    [Fact]
    public async Task ApproveAsync_OverlapsAnotherChange_ReturnsOverlapAndNoAuditEvent()
    {
        // Arrange
        var world = WorldWithReport();
        world.PriceChanges.Add(PriceChange.Approve(RoomSize.Single, -10m, new DateOnly(2027, 7, 1), new DateOnly(2027, 7, 31), "manager", world.Clock.UtcNow));

        // Act
        var result = await world.PriceSuggestionService().ApproveAsync(new PriceSuggestionApproveRequest(HighSingleKey), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(PriceChangeErrors.Overlap, result.FirstError!.Code);
        Assert.Single(world.PriceChanges.Items);
        Assert.Empty(world.StaffAudit.Events);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    /// <summary>Positive: shortening the dates to avoid the other change makes the approval succeed.</summary>
    [Fact]
    public async Task ApproveAsync_ShortenedAroundAnotherChange_Succeeds()
    {
        // Arrange
        var world = WorldWithReport();
        world.PriceChanges.Add(PriceChange.Approve(RoomSize.Single, -10m, new DateOnly(2027, 7, 1), new DateOnly(2027, 7, 31), "manager", world.Clock.UtcNow));

        // Act
        var result = await world.PriceSuggestionService().ApproveAsync(new PriceSuggestionApproveRequest(HighSingleKey, new DateOnly(2027, 8, 1), SummerLast), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, world.PriceChanges.Items.Count);
        Assert.Single(world.StaffAudit.Events);
    }

    /// <summary>Negative: a last day before the first day is refused with the period code and nothing is saved.</summary>
    [Fact]
    public async Task ApproveAsync_BackwardsDates_ReturnsThePeriodCode()
    {
        // Arrange
        var world = WorldWithReport();

        // Act
        var result = await world.PriceSuggestionService().ApproveAsync(new PriceSuggestionApproveRequest(HighSingleKey, SummerLast, SummerFirst), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(PriceChangeErrors.PeriodInvalid, result.FirstError!.Code);
        Assert.Empty(world.PriceChanges.Items);
        Assert.Empty(world.StaffAudit.Events);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    /// <summary>Negative: a request that is null is refused with an exception, as every service does.</summary>
    [Fact]
    public async Task ApproveAsync_NullRequest_Throws()
    {
        // Arrange
        var world = WorldWithReport();

        // Act and assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => world.PriceSuggestionService().ApproveAsync(null!, TestContext.Current.CancellationToken));
    }

    /// <summary>Positive: two approvals started at the same moment create one change when they are the same, and the lock is free afterwards.</summary>
    [Fact]
    public async Task ApproveAsync_TwoAtTheSameMoment_CreateOneChange()
    {
        // Arrange
        var world = WorldWithReport();
        var service = world.PriceSuggestionService();

        // Act
        var results = await Task.WhenAll(
            service.ApproveAsync(new PriceSuggestionApproveRequest(HighSingleKey), TestContext.Current.CancellationToken),
            service.ApproveAsync(new PriceSuggestionApproveRequest(HighSingleKey), TestContext.Current.CancellationToken));
        var after = await service.ApproveAsync(new PriceSuggestionApproveRequest(LowSingleKey), TestContext.Current.CancellationToken);

        // Assert
        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Single(results, result => result.Value.Created);
        Assert.True(after.IsSuccess);
        Assert.Equal(2, world.PriceChanges.Items.Count);
    }

    #endregion

    #region Reverting

    /// <summary>Positive: a change in force ends, the audit event names the change and the last day it counted.</summary>
    [Fact]
    public async Task RevertAsync_ChangeInForce_EndsItAndWritesOneAuditEvent()
    {
        // Arrange
        var world = WorldWithReport();
        var service = world.PriceSuggestionService();
        var approved = await service.ApproveAsync(new PriceSuggestionApproveRequest(HighSingleKey), TestContext.Current.CancellationToken);
        world.Clock.Today = new DateOnly(2027, 7, 10);

        // Act
        var result = await service.RevertAsync(approved.Value.Change.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(("Reverted", (DateOnly?)new DateOnly(2027, 7, 9)), (result.Value.State, result.Value.EndedOn));
        Assert.Equal(2, world.StaffAudit.Events.Count);
        var audit = world.StaffAudit.Events[1];
        Assert.Equal(StaffAuditAction.PriceChangeReverted, audit.Action);
        Assert.Equal($"#{approved.Value.Change.Id} Single +20.00% 2027-06-01..2027-08-31; ended 2027-07-09", audit.Detail);
        Assert.Equal("manager", audit.ActorName);
        Assert.Equal(2, world.UnitOfWork.SaveCount);
    }

    /// <summary>Positive: a change that has not begun is withdrawn, and the audit event says so.</summary>
    [Fact]
    public async Task RevertAsync_FutureChange_IsWithdrawn()
    {
        // Arrange
        var world = WorldWithReport();
        var service = world.PriceSuggestionService();
        var approved = await service.ApproveAsync(new PriceSuggestionApproveRequest(HighSingleKey), TestContext.Current.CancellationToken);

        // Act
        var result = await service.RevertAsync(approved.Value.Change.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.EndedOn);
        Assert.Equal($"#{approved.Value.Change.Id} Single +20.00% 2027-06-01..2027-08-31; withdrawn", world.StaffAudit.Events[1].Detail);
    }

    /// <summary>Negative: a second revert, an unknown id and a change whose period is past are refused and write no audit event.</summary>
    [Fact]
    public async Task RevertAsync_NotRevertable_ReturnsTheCodeAndWritesNoAuditEvent()
    {
        // Arrange
        var world = WorldWithReport();
        var service = world.PriceSuggestionService();
        var approved = await service.ApproveAsync(new PriceSuggestionApproveRequest(HighSingleKey), TestContext.Current.CancellationToken);
        var id = approved.Value.Change.Id;
        await service.RevertAsync(id, TestContext.Current.CancellationToken);
        var past = await service.ApproveAsync(new PriceSuggestionApproveRequest(LowSingleKey), TestContext.Current.CancellationToken);
        world.Clock.Today = new DateOnly(2028, 3, 1);
        var auditBefore = world.StaffAudit.Events.Count;
        var savesBefore = world.UnitOfWork.SaveCount;

        // Act
        var again = await service.RevertAsync(id, TestContext.Current.CancellationToken);
        var unknown = await service.RevertAsync(999, TestContext.Current.CancellationToken);
        var ended = await service.RevertAsync(past.Value.Change.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(PriceChangeErrors.AlreadyReverted, again.FirstError!.Code);
        Assert.Equal(PriceChangeErrors.NotFound, unknown.FirstError!.Code);
        Assert.Equal(PriceChangeErrors.PeriodEnded, ended.FirstError!.Code);
        Assert.Equal(auditBefore, world.StaffAudit.Events.Count);
        Assert.Equal(savesBefore, world.UnitOfWork.SaveCount);
    }

    #endregion

    #region Changes and effective prices

    /// <summary>Positive: the changes come back with the latest period first, then by size.</summary>
    [Fact]
    public async Task GetChangesAsync_SeveralChanges_AreOrderedNewestPeriodFirst()
    {
        // Arrange
        var world = WorldWithReport();
        var service = world.PriceSuggestionService();
        await service.ApproveAsync(new PriceSuggestionApproveRequest("double|2027-06-01|2027-08-31|20.00"), TestContext.Current.CancellationToken);
        await service.ApproveAsync(new PriceSuggestionApproveRequest(LowSingleKey), TestContext.Current.CancellationToken);
        await service.ApproveAsync(new PriceSuggestionApproveRequest(HighSingleKey), TestContext.Current.CancellationToken);

        // Act
        var result = await service.GetChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(
            [("Single", new DateOnly(2028, 1, 1)), ("Single", SummerFirst), ("Double", SummerFirst)],
            result.Value.Select(change => (change.Size, change.FirstDay)));
    }

    /// <summary>Positive: with nothing approved the list is empty.</summary>
    [Fact]
    public async Task GetChangesAsync_NothingApproved_ReturnsAnEmptyList()
    {
        // Arrange
        var world = WorldWithReport();

        // Act
        var result = await world.PriceSuggestionService().GetChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    /// <summary>Positive: the effective prices of a given day, and of today when no day is given.</summary>
    [Fact]
    public async Task GetEffectivePricesAsync_DayOrToday_UsesTheApprovedChange()
    {
        // Arrange
        var world = WorldWithReport();
        var service = world.PriceSuggestionService();
        await service.ApproveAsync(new PriceSuggestionApproveRequest(HighSingleKey), TestContext.Current.CancellationToken);
        world.Clock.Today = new DateOnly(2027, 7, 1);

        // Act
        var summer = await service.GetEffectivePricesAsync(null, TestContext.Current.CancellationToken);
        var after = await service.GetEffectivePricesAsync(new DateOnly(2027, 9, 1), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((120m, new DateOnly(2027, 7, 1)), (summer.Value.First(price => price.Size == "Single").Price, summer.Value[0].Day));
        Assert.Equal(100m, after.Value.First(price => price.Size == "Single").Price);
        Assert.Equal(150m, after.Value.First(price => price.Size == "Double").Price);
    }

    #endregion
}
