using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

using NFHotel.Web.Features.Booking;

namespace NFHotel.Web.Tests;

/// <summary>
/// The booking page answers with its booking box whatever the guests of its address hold (MIL-067 task 1), through the
/// real pipeline of the web application: a value that is not a whole number leaves the box at its default, where it
/// used to answer an error page (ZAP 30003, Integer Overflow Error).
/// </summary>
public sealed partial class BookingAddressTests : IClassFixture<WebFactory>
{
    /// <summary>The 44 digits that the DAST scan sent in the guests of the address.</summary>
    private const string ScanValue = "99999999999999999999999999999999999999999999";

    private readonly WebFactory _factory;

    public BookingAddressTests(WebFactory factory)
    {
        _factory = factory;
    }

    #region Guests that do not parse

    /// <summary>
    /// Negative: the number of the scan, the first number above the largest integer, a word, a decimal and an empty value
    /// each show the booking box with the default party of one.
    /// </summary>
    [Theory]
    [InlineData(ScanValue)]
    [InlineData("2147483648")]
    [InlineData("two")]
    [InlineData("2.5")]
    [InlineData("")]
    public async Task Get_GuestsThatDoNotParse_ShowsTheBoxWithOneGuest(string guests)
    {
        // Arrange
        using var browser = _factory.CreateBrowser();

        // Act
        using var response = await browser.GetAsync($"/book?guests={Uri.EscapeDataString(guests)}", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("When would you like to stay?", html, StringComparison.Ordinal);
        Assert.Equal(1, SelectedGuests(html));
    }

    /// <summary>Negative: with dates in the address, as the scan sent them, guests that do not parse leave the dates in the box.</summary>
    [Fact]
    public async Task Get_DatesWithGuestsThatDoNotParse_ShowsTheDates()
    {
        // Arrange
        using var browser = _factory.CreateBrowser();

        // Act
        using var response = await browser.GetAsync($"/book?checkIn=2020-01-01&checkOut=2020-01-02&guests={ScanValue}", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("2020-01-01", InputValue(html, "check-in"));
        Assert.Equal(1, SelectedGuests(html));
    }

    #endregion

    #region Guests that parse

    /// <summary>
    /// Boundary: a whole number fills the box, kept between one and the most the box offers; the largest integer is a
    /// number like any other and is kept at the most.
    /// </summary>
    [Theory]
    [InlineData("3", 3)]
    [InlineData("1", 1)]
    [InlineData("4", BookingFlowViewModel.MaxGuests)]
    [InlineData("5", BookingFlowViewModel.MaxGuests)]
    [InlineData("0", 1)]
    [InlineData("-1", 1)]
    [InlineData("2147483647", BookingFlowViewModel.MaxGuests)]
    public async Task Get_WholeNumberOfGuests_FillsTheBoxWithinItsBounds(string guests, int shown)
    {
        // Arrange
        using var browser = _factory.CreateBrowser();

        // Act
        using var response = await browser.GetAsync($"/book?guests={guests}", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(shown, SelectedGuests(html));
    }

    #endregion

    #region Helpers

    /// <summary>The value of the selected option of the guests field, or <c>null</c> when none is selected.</summary>
    private static int? SelectedGuests(string html)
    {
        var select = GuestsField().Match(html);
        Assert.True(select.Success, "The page has no guests field.");
        var selected = OptionTag().Matches(select.Value)
            .Select(option => option.Value)
            .FirstOrDefault(option => option.Contains(" selected", StringComparison.Ordinal));
        return selected is null ? null : int.Parse(ValueAttribute().Match(selected).Groups["value"].Value, CultureInfo.InvariantCulture);
    }

    /// <summary>The value attribute of the input with an id.</summary>
    private static string InputValue(string html, string id)
    {
        var input = InputTag().Matches(html).Select(tag => tag.Value).Single(tag => tag.Contains($"id=\"{id}\"", StringComparison.Ordinal));
        return ValueAttribute().Match(input).Groups["value"].Value;
    }

    [GeneratedRegex("""<select[^>]*\bid="guests"[^>]*>.*?</select>""", RegexOptions.Singleline)]
    private static partial Regex GuestsField();

    [GeneratedRegex("<option[^>]*>")]
    private static partial Regex OptionTag();

    [GeneratedRegex("<input[^>]*>")]
    private static partial Regex InputTag();

    [GeneratedRegex("""\bvalue="(?<value>[^"]*)"\s*""")]
    private static partial Regex ValueAttribute();

    #endregion
}
