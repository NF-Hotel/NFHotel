using AngleSharp.Dom;

using Bunit;

using NFHotel.Application.Dto.RoomRates;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.RoomRates;
using NFHotel.Web.Features.Admin.Pricing;
using NFHotel.Web.Security;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>The room rates page of the Manager, rendered: one row per room size with its rate and a field for a new one (MIL-055).</summary>
public sealed class RoomRatesPageTests : IAsyncDisposable
{
    private readonly IRoomRateService _service = Substitute.For<IRoomRateService>();
    private readonly PageHost _host = new();

    public RoomRatesPageTests()
    {
        _host.With(new RoomRatesViewModel(new PassThroughRunner<IRoomRateService>(_service)));
        _host.SignedInWith(StaffPolicies.ManagePrices);
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    /// <summary>Positive: a size with a rate shows the price, who set it and when; a size without one says it is not set; each size has its own field.</summary>
    [Fact]
    public void Render_OneRateSet_ShowsItAndMarksTheOthersNotSet()
    {
        // Arrange
        RoomRateDto single = new("Single", 80m, "manager", new DateTimeOffset(2026, 11, 1, 10, 0, 0, TimeSpan.Zero));
        _service.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<RoomRateDto>>.Success([single]));

        // Act
        IRenderedComponent<RoomRates> page = _host.Render<RoomRates>();

        // Assert
        Assert.Equal(["80.00", "manager", "2026-11-01 10:00"], CellsOf(page, "Single room")[..3]);
        Assert.Equal(["not set", "-", "-"], CellsOf(page, "Double room")[..3]);
        Assert.Equal(["not set", "-", "-"], CellsOf(page, "Suite")[..3]);
        Assert.Equal("80.00", page.Find("#rate-single").GetAttribute("value"));
        Assert.Equal(["rate-single", "rate-double", "rate-suite"], page.FindAll("input[id^='rate-']").Select(input => input.Id));
    }

    /// <summary>The texts of the cells of the row whose header is the room size.</summary>
    private static string[] CellsOf(IRenderedComponent<RoomRates> page, string size)
    {
        IElement row = page.FindAll("tbody tr").Single(candidate => candidate.QuerySelector("th")?.TextContent.Trim() == size);
        return [.. row.QuerySelectorAll("td").Select(cell => cell.TextContent.Trim())];
    }
}
