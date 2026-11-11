using Bunit;

using NFHotel.Application.Dto.Pricing;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Pricing;
using NFHotel.TestSupport;
using NFHotel.Web.Features.Admin.Pricing;
using NFHotel.Web.Security;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>The price suggestions page of the Manager, rendered: the suggestions of the service with their texts and labels (MIL-071 task 4).</summary>
public sealed class PriceSuggestionsPageTests : IAsyncDisposable
{
    private static readonly DateOnly Today = new(2026, 11, 11);

    private readonly IPriceSuggestionService _service = Substitute.For<IPriceSuggestionService>();
    private readonly PageHost _host = new();

    public PriceSuggestionsPageTests()
    {
        _service.GetChangesAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<PriceChangeDto>>.Success([]));
        _service.GetEffectivePricesAsync(Arg.Any<DateOnly?>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<EffectivePriceDto>>.Success([]));
        _host.With(new PriceSuggestionsViewModel(new PassThroughRunner<IPriceSuggestionService>(_service), new FakeClock { Today = Today }));
        _host.SignedInWith(StaffPolicies.ManagePrices);
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    /// <summary>Positive: the holiday, the earlier years, the labelled explanation and the labelled idea are on the page, and the Approve button stays.</summary>
    [Fact]
    public void Render_Suggestions_ShowHolidayHistoryAndLabelledTexts()
    {
        // Arrange
        PriceSuggestionItemDto item = new(
            "single|2026-12-24|2026-12-26|20.00", "Single", "High", new DateOnly(2026, 12, 24), new DateOnly(2026, 12, 26), 20m, 100m, 120m, 86.5m, 80m, false,
            "holiday", "Christmas", [new PriceSuggestionHistoryDto(2024, 85m), new PriceSuggestionHistoryDto(2025, 88m)], new PriceSuggestionTextDto("Busy in both earlier years.", true));
        PriceSuggestionListDto list = new(PriceSuggestionBasis.Ok, Today, [item], null, [new PriceSuggestionTextDto("Offer a package for the quiet weeks.", true)]);
        _service.GetOpenAsync(Arg.Any<CancellationToken>()).Returns(Result<PriceSuggestionListDto>.Success(list));

        // Act
        IRenderedComponent<PriceSuggestions> page = _host.Render<PriceSuggestions>();

        // Assert
        string markup = page.Markup;
        Assert.Contains("Holiday: Christmas", markup, StringComparison.Ordinal);
        Assert.Contains("2024: 85 %, 2025: 88 %", markup, StringComparison.Ordinal);
        Assert.Contains("Busy in both earlier years.", markup, StringComparison.Ordinal);
        Assert.Contains("Offer a package for the quiet weeks.", markup, StringComparison.Ordinal);
        Assert.Equal(2, markup.Split(PriceSuggestionsViewModel.AiLabel).Length - 1);
        Assert.Contains("100.00 → 120.00", markup, StringComparison.Ordinal);
        Assert.Contains(page.FindAll("button"), button => button.TextContent.Trim() == "Approve");
    }

    /// <summary>Negative: without a report of the service the page says so and offers no approval (criterion 7 of MIL-071).</summary>
    [Fact]
    public void Render_NoBasis_SaysSoAndOffersNoApproval()
    {
        // Arrange
        _service.GetOpenAsync(Arg.Any<CancellationToken>())
            .Returns(Result<PriceSuggestionListDto>.Success(new PriceSuggestionListDto(PriceSuggestionBasis.NoBasis, Today, [], null)));

        // Act
        IRenderedComponent<PriceSuggestions> page = _host.Render<PriceSuggestions>();

        // Assert
        Assert.Contains("Nothing can be approved until then", page.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain(page.FindAll("button"), button => button.TextContent.Trim() == "Approve");
    }

    /// <summary>Boundary: a report of the service with no suggestion says that no holiday or week needs a change, and shows no approval.</summary>
    [Fact]
    public void Render_NoSuggestions_SaysNothingNeedsAChange()
    {
        // Arrange
        _service.GetOpenAsync(Arg.Any<CancellationToken>())
            .Returns(Result<PriceSuggestionListDto>.Success(new PriceSuggestionListDto(PriceSuggestionBasis.Ok, Today, [], null)));

        // Act
        IRenderedComponent<PriceSuggestions> page = _host.Render<PriceSuggestions>();

        // Assert
        Assert.Contains("no holiday or week in the next two months", page.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain(page.FindAll("button"), button => button.TextContent.Trim() == "Approve");
    }
}
