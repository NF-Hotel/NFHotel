using NFHotel.Application.Dto.Pricing;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Pricing;
using NFHotel.Domain.Errors;
using NFHotel.TestSupport;
using NFHotel.Web.Features.Admin.Pricing;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>The presentation logic of the price suggestions page (MIL-055).</summary>
public sealed class PriceSuggestionsViewModelTests
{
    private const string Key = "Single|High|2026-12-01|2027-02-28";
    private static readonly DateOnly Today = new(2026, 11, 2);
    private static readonly DateTimeOffset At = new(2026, 11, 1, 10, 0, 0, TimeSpan.Zero);

    #region Load

    /// <summary>Positive: the three lists are filled and the dates are prefilled.</summary>
    [Fact]
    public async Task LoadAsync_AllThreeCallsSucceed_FillsTheThreeLists()
    {
        // Arrange
        (PriceSuggestionsViewModel? viewModel, IPriceSuggestionService _) = Build();

        // Act
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(viewModel.IsLoaded);
        Assert.False(viewModel.IsBusy);
        Assert.Empty(viewModel.Errors);
        PriceSuggestionsViewModel.SuggestionRow row = Assert.Single(viewModel.Suggestions);
        Assert.Equal("Single", row.Size);
        Assert.Equal("High season", row.Season);
        Assert.Equal("2026-12-01", viewModel.FirstDayOf(Key));
        Assert.Equal("2027-02-28", viewModel.LastDayOf(Key));
        Assert.Single(viewModel.Changes);
        Assert.Equal(3, viewModel.EffectivePrices.Count);
        Assert.False(viewModel.HasNoBasis);
        Assert.False(viewModel.HasNoSuggestions);
    }

    /// <summary>Positive: the basis no_basis is exposed and the list is empty.</summary>
    [Fact]
    public async Task LoadAsync_NoBasis_ExposesTheNoBasisState()
    {
        // Arrange
        (PriceSuggestionsViewModel? viewModel, IPriceSuggestionService? service) = Build();
        service.GetOpenAsync(Arg.Any<CancellationToken>()).Returns(
            Result<PriceSuggestionListDto>.Success(new PriceSuggestionListDto(PriceSuggestionBasis.NoBasis, Today, [], null)));

        // Act
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(viewModel.HasNoBasis);
        Assert.False(viewModel.HasNoSuggestions);
        Assert.Empty(viewModel.Suggestions);
    }

    /// <summary>Positive: a basis with no items is the empty state, not the no-basis state.</summary>
    [Fact]
    public async Task LoadAsync_BasisOkAndNoItems_ExposesTheEmptyState()
    {
        // Arrange
        (PriceSuggestionsViewModel? viewModel, IPriceSuggestionService? service) = Build();
        service.GetOpenAsync(Arg.Any<CancellationToken>()).Returns(
            Result<PriceSuggestionListDto>.Success(new PriceSuggestionListDto(PriceSuggestionBasis.Ok, Today, [], null)));

        // Act
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(viewModel.HasNoSuggestions);
        Assert.False(viewModel.HasNoBasis);
    }

    /// <summary>Negative: a service that is down shows the text of the code and ends the loading state.</summary>
    [Fact]
    public async Task LoadAsync_ServiceDown_ShowsTheTextAndIsLoaded()
    {
        // Arrange
        (PriceSuggestionsViewModel? viewModel, IPriceSuggestionService? service) = Build();
        service.GetOpenAsync(Arg.Any<CancellationToken>()).Returns(Result<PriceSuggestionListDto>.Failure(ErrorCodes.Api.Unavailable));

        // Act
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(viewModel.IsLoaded);
        Assert.Single(viewModel.Errors);
        Assert.False(viewModel.HasNoSuggestions);
        Assert.Single(viewModel.Changes);
    }

    /// <summary>Positive: the AI text is exposed as a separate text and is in no row.</summary>
    [Fact]
    public async Task LoadAsync_AiExplanationGiven_ExposesItApartFromTheRows()
    {
        // Arrange
        (PriceSuggestionsViewModel? viewModel, IPriceSuggestionService _) = Build();

        // Act
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("Summer is busy.", viewModel.AiExplanation);
        Assert.DoesNotContain("Summer is busy.", viewModel.Suggestions.Single().Basis, StringComparison.Ordinal);
    }

    /// <summary>Negative: without an AI text there is no explanation.</summary>
    [Fact]
    public async Task LoadAsync_NoAiExplanation_ExposesNull()
    {
        // Arrange
        (PriceSuggestionsViewModel? viewModel, IPriceSuggestionService? service) = Build();
        service.GetOpenAsync(Arg.Any<CancellationToken>()).Returns(Result<PriceSuggestionListDto>.Success(OpenList(null)));

        // Act
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(viewModel.AiExplanation);
    }

    #endregion

    #region Approve

    /// <summary>Positive: unchanged dates are sent as null, with the key, and the lists are reloaded.</summary>
    [Fact]
    public async Task ApproveAsync_DatesUnchanged_SendsTheKeyAndNullDates()
    {
        // Arrange
        (PriceSuggestionsViewModel? viewModel, IPriceSuggestionService? service) = Build();
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        // Act
        await viewModel.ApproveAsync(Key, TestContext.Current.CancellationToken);

        // Assert
        await service.Received(1).ApproveAsync(
            Arg.Is<PriceSuggestionApproveRequest>(request => request.Key == Key && request.FirstDay == null && request.LastDay == null),
            Arg.Any<CancellationToken>());
        await service.Received(2).GetOpenAsync(Arg.Any<CancellationToken>());
        Assert.Equal("Approved: Single +20 % from 2026-12-01 to 2027-02-28", viewModel.SuccessMessage);
        Assert.Empty(viewModel.Errors);
    }

    /// <summary>Positive: changed dates are sent.</summary>
    [Fact]
    public async Task ApproveAsync_DatesChanged_SendsTheDates()
    {
        // Arrange
        (PriceSuggestionsViewModel? viewModel, IPriceSuggestionService? service) = Build();
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.SetFirstDay(Key, "2026-12-15");
        viewModel.SetLastDay(Key, "2027-01-31");

        // Act
        await viewModel.ApproveAsync(Key, TestContext.Current.CancellationToken);

        // Assert
        await service.Received(1).ApproveAsync(
            Arg.Is<PriceSuggestionApproveRequest>(request =>
                request.FirstDay == new DateOnly(2026, 12, 15) && request.LastDay == new DateOnly(2027, 1, 31)),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Negative: a second call while the first runs is ignored.</summary>
    [Fact]
    public async Task ApproveAsync_WhileRunning_IgnoresTheSecondCall()
    {
        // Arrange
        (PriceSuggestionsViewModel? viewModel, IPriceSuggestionService? service) = Build();
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        TaskCompletionSource<Result<ApprovedPriceChange>> gate = new();
        service.ApproveAsync(Arg.Any<PriceSuggestionApproveRequest>(), Arg.Any<CancellationToken>()).Returns(gate.Task);

        // Act
        Task first = viewModel.ApproveAsync(Key, TestContext.Current.CancellationToken);
        bool busy = viewModel.IsBusy;
        await viewModel.ApproveAsync(Key, TestContext.Current.CancellationToken);
        gate.SetResult(Result<ApprovedPriceChange>.Success(new ApprovedPriceChange(Change(1, "Approved", null), true)));
        await first;

        // Assert
        Assert.True(busy);
        await service.Received(1).ApproveAsync(Arg.Any<PriceSuggestionApproveRequest>(), Arg.Any<CancellationToken>());
        Assert.False(viewModel.IsBusy);
    }

    /// <summary>Negative: an overlap shows the mapped message, no success, and keeps the list.</summary>
    [Fact]
    public async Task ApproveAsync_Overlap_ShowsTheMessageAndKeepsTheList()
    {
        // Arrange
        (PriceSuggestionsViewModel? viewModel, IPriceSuggestionService? service) = Build();
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        service.ApproveAsync(Arg.Any<PriceSuggestionApproveRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<ApprovedPriceChange>.Failure(PriceChangeErrors.Overlap));

        // Act
        await viewModel.ApproveAsync(Key, TestContext.Current.CancellationToken);

        // Assert
        string error = Assert.Single(viewModel.Errors);
        Assert.StartsWith("Another price change of the same room size", error, StringComparison.Ordinal);
        Assert.Null(viewModel.SuccessMessage);
        Assert.Single(viewModel.Suggestions);
        Assert.False(viewModel.IsBusy);
    }

    /// <summary>Negative: a date that is not a date is refused before any call.</summary>
    [Fact]
    public async Task ApproveAsync_EmptyDate_ShowsThePeriodMessageAndCallsNothing()
    {
        // Arrange
        (PriceSuggestionsViewModel? viewModel, IPriceSuggestionService? service) = Build();
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        viewModel.SetLastDay(Key, string.Empty);

        // Act
        await viewModel.ApproveAsync(Key, TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(viewModel.Errors);
        await service.DidNotReceive().ApproveAsync(Arg.Any<PriceSuggestionApproveRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Negative: a key that is no longer in the list shows the not-found message.</summary>
    [Fact]
    public async Task ApproveAsync_UnknownKey_ShowsTheNotFoundMessage()
    {
        // Arrange
        (PriceSuggestionsViewModel? viewModel, IPriceSuggestionService? service) = Build();
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        // Act
        await viewModel.ApproveAsync("nope", TestContext.Current.CancellationToken);

        // Assert
        Assert.StartsWith("The suggestion is no longer open", viewModel.Errors.Single(), StringComparison.Ordinal);
        await service.DidNotReceive().ApproveAsync(Arg.Any<PriceSuggestionApproveRequest>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Revert

    /// <summary>Positive: a revert of a change that was in force says Reverted and reloads.</summary>
    [Fact]
    public async Task RevertAsync_ChangeThatWasInForce_SaysRevertedAndReloads()
    {
        // Arrange
        (PriceSuggestionsViewModel? viewModel, IPriceSuggestionService? service) = Build();
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        service.RevertAsync(7, Arg.Any<CancellationToken>())
            .Returns(Result<PriceChangeDto>.Success(Change(7, "Reverted", new DateOnly(2026, 11, 1))));

        // Act
        await viewModel.RevertAsync(7, TestContext.Current.CancellationToken);

        // Assert
        await service.Received(1).RevertAsync(7, Arg.Any<CancellationToken>());
        await service.Received(2).GetChangesAsync(Arg.Any<CancellationToken>());
        Assert.StartsWith("Reverted:", viewModel.SuccessMessage, StringComparison.Ordinal);
        Assert.False(viewModel.IsBusy);
    }

    /// <summary>Positive: a revert of a change that had not begun says Withdrawn.</summary>
    [Fact]
    public async Task RevertAsync_ChangeThatHadNotBegun_SaysWithdrawn()
    {
        // Arrange
        (PriceSuggestionsViewModel? viewModel, IPriceSuggestionService? service) = Build();
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        service.RevertAsync(7, Arg.Any<CancellationToken>()).Returns(Result<PriceChangeDto>.Success(Change(7, "Reverted", null)));

        // Act
        await viewModel.RevertAsync(7, TestContext.Current.CancellationToken);

        // Assert
        Assert.StartsWith("Withdrawn:", viewModel.SuccessMessage, StringComparison.Ordinal);
    }

    /// <summary>Negative: a refused revert shows the mapped message.</summary>
    [Fact]
    public async Task RevertAsync_AlreadyReverted_ShowsTheMessage()
    {
        // Arrange
        (PriceSuggestionsViewModel? viewModel, IPriceSuggestionService? service) = Build();
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        service.RevertAsync(7, Arg.Any<CancellationToken>()).Returns(Result<PriceChangeDto>.Failure(PriceChangeErrors.AlreadyReverted));

        // Act
        await viewModel.RevertAsync(7, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["The price change is already reverted."], viewModel.Errors);
        Assert.Null(viewModel.SuccessMessage);
    }

    /// <summary>Negative: a revert while a call runs is ignored.</summary>
    [Fact]
    public async Task RevertAsync_WhileRunning_IgnoresTheSecondCall()
    {
        // Arrange
        (PriceSuggestionsViewModel? viewModel, IPriceSuggestionService? service) = Build();
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);
        TaskCompletionSource<Result<PriceChangeDto>> gate = new();
        service.RevertAsync(7, Arg.Any<CancellationToken>()).Returns(gate.Task);

        // Act
        Task first = viewModel.RevertAsync(7, TestContext.Current.CancellationToken);
        await viewModel.RevertAsync(7, TestContext.Current.CancellationToken);
        gate.SetResult(Result<PriceChangeDto>.Success(Change(7, "Reverted", new DateOnly(2026, 11, 1))));
        await first;

        // Assert
        await service.Received(1).RevertAsync(7, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Derived texts

    /// <summary>Equivalence: the five states follow from the state, the end day and today.</summary>
    [Theory]
    [InlineData("Approved", "2026-10-01", "2026-12-31", null, PriceSuggestionsViewModel.ChangeStatus.InForce, "In force")]
    [InlineData("Approved", "2026-11-02", "2026-11-02", null, PriceSuggestionsViewModel.ChangeStatus.InForce, "In force")]
    [InlineData("Approved", "2026-11-03", "2026-12-31", null, PriceSuggestionsViewModel.ChangeStatus.Coming, "Coming")]
    [InlineData("Approved", "2026-09-01", "2026-11-01", null, PriceSuggestionsViewModel.ChangeStatus.Finished, "Finished")]
    [InlineData("Reverted", "2026-10-01", "2026-12-31", "2026-11-01", PriceSuggestionsViewModel.ChangeStatus.Reverted, "Reverted")]
    [InlineData("Reverted", "2026-12-01", "2027-02-28", null, PriceSuggestionsViewModel.ChangeStatus.Withdrawn, "Withdrawn")]
    public void StatusOf_EachState_IsDerivedFromTheChangeAndToday(
        string state,
        string first,
        string last,
        string? endedOn,
        PriceSuggestionsViewModel.ChangeStatus expected,
        string text)
    {
        // Arrange
        PriceChangeDto change = new(
            1,
            "Single",
            20m,
            DateOnly.Parse(first, System.Globalization.CultureInfo.InvariantCulture),
            DateOnly.Parse(last, System.Globalization.CultureInfo.InvariantCulture),
            state,
            "manager",
            At,
            state == "Reverted" ? "manager" : null,
            state == "Reverted" ? At : null,
            endedOn is null ? null : DateOnly.Parse(endedOn, System.Globalization.CultureInfo.InvariantCulture));

        // Act
        PriceSuggestionsViewModel.ChangeStatus status = PriceSuggestionsViewModel.StatusOf(change, Today);

        // Assert
        Assert.Equal(expected, status);
        Assert.Equal(text, PriceSuggestionsViewModel.Describe(status));
    }

    /// <summary>Positive: only changes that are in force or coming can be reverted, and the state text uses the clock.</summary>
    [Fact]
    public async Task LoadAsync_ChangesInEveryState_AllowsRevertOnlyForInForceAndComing()
    {
        // Arrange
        (PriceSuggestionsViewModel? viewModel, IPriceSuggestionService? service) = Build();
        service.GetChangesAsync(Arg.Any<CancellationToken>()).Returns(
            Result<IReadOnlyList<PriceChangeDto>>.Success(
            [
                Change(1, "Approved", null, new DateOnly(2026, 12, 1), new DateOnly(2027, 1, 31)),
                Change(2, "Approved", null, new DateOnly(2026, 10, 1), new DateOnly(2026, 12, 31)),
                Change(3, "Approved", null, new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 30)),
                Change(4, "Reverted", new DateOnly(2026, 11, 1), new DateOnly(2026, 10, 1), new DateOnly(2026, 12, 31)),
                Change(5, "Reverted", null, new DateOnly(2026, 12, 1), new DateOnly(2027, 1, 31)),
            ]));

        // Act
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["Coming", "In force", "Finished", "Reverted", "Withdrawn"], viewModel.Changes.Select(change => change.State));
        Assert.Equal([true, true, false, false, false], viewModel.Changes.Select(change => change.CanRevert));
    }

    /// <summary>Positive: the effective rows show base rate, change and price, and "not set" gives null values.</summary>
    [Fact]
    public async Task LoadAsync_OneSizeWithoutBaseRate_ShowsNullValuesForIt()
    {
        // Arrange
        (PriceSuggestionsViewModel? viewModel, IPriceSuggestionService _) = Build();

        // Act
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        // Assert
        PriceSuggestionsViewModel.EffectiveRow single = viewModel.EffectivePrices.Single(row => row.Size == "Single");
        Assert.Equal("100.00", single.BaseRate);
        Assert.Equal("+20 %", single.Change);
        Assert.Equal("120.00", single.Price);
        PriceSuggestionsViewModel.EffectiveRow suite = viewModel.EffectivePrices.Single(row => row.Size == "Suite");
        Assert.Null(suite.BaseRate);
        Assert.Null(suite.Price);
        Assert.Equal("-", suite.Change);
    }

    /// <summary>Positive: today is asked for from the clock, never from the system time.</summary>
    [Fact]
    public async Task LoadAsync_Always_AsksForTheEffectivePricesOfTheClockDay()
    {
        // Arrange
        (PriceSuggestionsViewModel? viewModel, IPriceSuggestionService? service) = Build();

        // Act
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        // Assert
        await service.Received(1).GetEffectivePricesAsync(Today, Arg.Any<CancellationToken>());
    }

    /// <summary>Equivalence: changes get a sign, the minus is the true minus sign, and whole numbers have no decimals.</summary>
    [Theory]
    [InlineData(20, "+20 %")]
    [InlineData(-15, "−15 %")]
    [InlineData(12.5, "+12.5 %")]
    public void FormatChange_SignedPercentage_HasSignAndNoNeedlessDecimals(double percentage, string expected)
    {
        Assert.Equal(expected, PriceSuggestionsViewModel.FormatChange((decimal)percentage));
    }

    /// <summary>Positive: the price move uses two decimals and the invariant culture.</summary>
    [Fact]
    public void FormatMove_TwoPrices_ShowsOldArrowNew()
    {
        Assert.Equal("100.00 → 120.00", PriceSuggestionsViewModel.FormatMove(100m, 120m));
    }

    /// <summary>Positive: the row of a high-season suggestion names the occupancy and the threshold it rests on.</summary>
    [Fact]
    public async Task LoadAsync_HighAndLowSeasonItems_ShowTheFiguresTheyRestOn()
    {
        // Arrange
        (PriceSuggestionsViewModel? viewModel, IPriceSuggestionService? service) = Build();
        PriceSuggestionItemDto low = new("low", "Double", "Low", new DateOnly(2027, 1, 1), new DateOnly(2027, 1, 31), -15m, 100m, 85m, 35m, 40m, true);
        service.GetOpenAsync(Arg.Any<CancellationToken>()).Returns(
            Result<PriceSuggestionListDto>.Success(OpenList("x", [HighItem(), low])));

        // Act
        await viewModel.LoadAsync(TestContext.Current.CancellationToken);

        // Assert
        PriceSuggestionsViewModel.SuggestionRow high = viewModel.Suggestions[0];
        Assert.Equal("+20 %", high.Change);
        Assert.Equal("100.00 → 120.00", high.Move);
        Assert.Equal("Occupancy 85.0 % at or above 80 %", high.Basis);
        Assert.False(high.WasLimited);
        PriceSuggestionsViewModel.SuggestionRow lowRow = viewModel.Suggestions[1];
        Assert.Equal("Low season", lowRow.Season);
        Assert.Equal("−15 %", lowRow.Change);
        Assert.Equal("100.00 → 85.00", lowRow.Move);
        Assert.Equal("Occupancy 35.0 % at or below 40 %", lowRow.Basis);
        Assert.True(lowRow.WasLimited);
    }

    #endregion

    #region Helpers

    private static (PriceSuggestionsViewModel ViewModel, IPriceSuggestionService Service) Build()
    {
        IPriceSuggestionService service = Substitute.For<IPriceSuggestionService>();
        service.GetOpenAsync(Arg.Any<CancellationToken>()).Returns(Result<PriceSuggestionListDto>.Success(OpenList("Summer is busy.")));
        service.GetChangesAsync(Arg.Any<CancellationToken>()).Returns(
            Result<IReadOnlyList<PriceChangeDto>>.Success([Change(1, "Approved", null)]));
        service.GetEffectivePricesAsync(Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>()).Returns(
            Result<IReadOnlyList<EffectivePriceDto>>.Success(
            [
                new EffectivePriceDto("Single", Today, 100m, 120m, 20m),
                new EffectivePriceDto("Double", Today, 150m, 150m, null),
                new EffectivePriceDto("Suite", Today, null, null, null),
            ]));
        service.ApproveAsync(Arg.Any<PriceSuggestionApproveRequest>(), Arg.Any<CancellationToken>()).Returns(
            Result<ApprovedPriceChange>.Success(new ApprovedPriceChange(Change(1, "Approved", null), true)));
        PriceSuggestionsViewModel viewModel = new(new PassThroughRunner<IPriceSuggestionService>(service), new FakeClock { Today = Today });
        return (viewModel, service);
    }

    private static PriceSuggestionItemDto HighItem() =>
        new(Key, "Single", "High", new DateOnly(2026, 12, 1), new DateOnly(2027, 2, 28), 20m, 100m, 120m, 85m, 80m, false);

    private static PriceSuggestionListDto OpenList(string? ai, IReadOnlyList<PriceSuggestionItemDto>? items = null) =>
        new(PriceSuggestionBasis.Ok, Today, items ?? [HighItem()], ai);

    private static PriceChangeDto Change(int id, string state, DateOnly? endedOn, DateOnly? first = null, DateOnly? last = null) =>
        new(
            id,
            "Single",
            20m,
            first ?? new DateOnly(2026, 12, 1),
            last ?? new DateOnly(2027, 2, 28),
            state,
            "manager",
            At,
            state == "Reverted" ? "manager" : null,
            state == "Reverted" ? At : null,
            endedOn);

    #endregion
}
