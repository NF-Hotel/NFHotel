using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Web.Features.Admin.Shared;

namespace NFHotel.Web.Tests;

/// <summary>The arithmetic behind the SVG charts: scales, label thinning, donut slices, colours and the month grid.</summary>
public sealed class ChartGeometryTests
{
    #region Scale

    /// <summary>Positive: the axis limits are round and hold the values.</summary>
    [Theory]
    [InlineData(0, 47.3, 0, 60)]
    [InlineData(0, 100, 0, 100)]
    [InlineData(0, 8, 0, 8)]
    [InlineData(0, 1234, 0, 1500)]
    public void Nice_Values_GivesRoundLimits(double min, double max, double expectedMin, double expectedMax)
    {
        // Act
        NiceAxis axis = LinearScale.Nice(min, max);

        // Assert
        Assert.Equal((expectedMin, expectedMax), (axis.Min, axis.Max));
        Assert.Equal(axis.Min, axis.Ticks[0]);
        Assert.Equal(axis.Max, axis.Ticks[^1]);
        Assert.InRange(axis.Ticks.Count, 3, 9);
    }

    /// <summary>Boundary: values that are all the same, or all zero, still give an axis with a width.</summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(5, 5)]
    public void Nice_NoSpread_GivesAnAxisWithAWidth(double min, double max)
    {
        // Act
        NiceAxis axis = LinearScale.Nice(min, max);

        // Assert
        Assert.True(axis.Max > axis.Min);
        Assert.True(axis.Ticks.Count >= 2);
    }

    /// <summary>Positive: a vertical scale puts the lowest value at the bottom pixel and the highest at the top pixel.</summary>
    [Fact]
    public void Map_VerticalScale_PutsZeroAtTheBottom()
    {
        // Arrange
        LinearScale scale = new LinearScale(0, 50, 200, 20);

        // Act and assert
        Assert.Equal(200, scale.Map(0));
        Assert.Equal(20, scale.Map(50));
        Assert.Equal(110, scale.Map(25));
    }

    /// <summary>Boundary: a scale with no width maps everything to the middle.</summary>
    [Fact]
    public void Map_NoWidth_IsTheMiddle()
    {
        Assert.Equal(60, new LinearScale(3, 3, 20, 100).Map(3));
    }

    #endregion

    #region Labels

    /// <summary>Positive: 30 months with room for 12 labels give every third label, and 10 points give all.</summary>
    [Theory]
    [InlineData(10, 12, 1)]
    [InlineData(12, 12, 1)]
    [InlineData(13, 12, 2)]
    [InlineData(30, 12, 3)]
    [InlineData(60, 12, 5)]
    public void Step_ManyLabels_IsEveryNth(int count, int maxLabels, int expected)
    {
        Assert.Equal(expected, LabelThinner.Step(count, maxLabels));
    }

    /// <summary>Positive: with a gap of 3 the first, fourth, seventh and so on are written.</summary>
    [Fact]
    public void IsShown_GapOfThree_WritesEveryThirdLabel()
    {
        // Act
        List<int> shown = Enumerable.Range(0, 30).Where(index => LabelThinner.IsShown(index, 3)).ToList();

        // Assert
        Assert.Equal(10, shown.Count);
        Assert.Equal([0, 3, 6], shown.Take(3));
    }

    #endregion

    #region Donut

    /// <summary>Positive: two equal parts are two half rings that start at twelve o'clock, the first on the right.</summary>
    [Fact]
    public void Slices_TwoEqualParts_AreHalfRings()
    {
        // Act
        IReadOnlyList<DonutSlice> slices = DonutGeometry.Slices([50, 50], 100, 100, 90, 50);

        // Assert
        Assert.Equal([0.5, 0.5], slices.Select(slice => slice.Share));
        Assert.All(slices, slice => Assert.StartsWith("M ", slice.Path, StringComparison.Ordinal));
        Assert.True(slices[0].LabelX > 100);
        Assert.True(slices[1].LabelX < 100);
    }

    /// <summary>Boundary: a part of 0 has no path, and a part of 100 % is drawn as nearly the whole ring.</summary>
    [Fact]
    public void Slices_OneWholePart_IsDrawnAndEmptyPartIsNot()
    {
        // Act
        IReadOnlyList<DonutSlice> slices = DonutGeometry.Slices([100, 0], 100, 100, 90, 50);

        // Assert
        Assert.NotEmpty(slices[0].Path);
        Assert.Contains(" 0 1 1 ", slices[0].Path, StringComparison.Ordinal);
        Assert.Empty(slices[1].Path);
    }

    /// <summary>Boundary: values that add up to 0 give no slice.</summary>
    [Fact]
    public void Slices_AllZero_GivesNothing()
    {
        Assert.Empty(DonutGeometry.Slices([0, 0], 100, 100, 90, 50));
    }

    #endregion

    #region Colour

    /// <summary>Positive: the diverging scale is blue at -1, near white at 0 and red at +1, and is cut at the ends.</summary>
    [Fact]
    public void Diverging_Values_AreBlueWhiteRed()
    {
        Assert.Equal("#2166ac", HeatColor.Diverging(-1));
        Assert.Equal("#f7f7f7", HeatColor.Diverging(0));
        Assert.Equal("#b2182b", HeatColor.Diverging(1));
        Assert.Equal("#b2182b", HeatColor.Diverging(3));
        Assert.Equal("#2166ac", HeatColor.Diverging(-3));
    }

    /// <summary>Positive: the middle of the scale is lighter than its ends, so that near 0 reads as no correlation.</summary>
    [Fact]
    public void Diverging_NearZero_IsCloserToWhiteThanToTheEnds()
    {
        // Act
        string near = HeatColor.Diverging(0.1);

        // Assert: the red channel of a blue shade falls, and the blue channel of a red shade falls, from white.
        Assert.True(Convert.ToInt32(near[1..3], 16) > 0xe0);
        Assert.True(Convert.ToInt32(near[5..7], 16) > 0xe0);
    }

    /// <summary>Positive: the text on a dark fill is white and on a light fill is dark, whatever the page theme.</summary>
    [Theory]
    [InlineData("#2166ac", "#ffffff")]
    [InlineData("#b2182b", "#ffffff")]
    [InlineData("#f7f7f7", "#14171f")]
    [InlineData("#eff3ff", "#14171f")]
    [InlineData("#08519c", "#ffffff")]
    public void TextOn_Fill_IsReadable(string fill, string expected)
    {
        Assert.Equal(expected, HeatColor.TextOn(fill));
    }

    /// <summary>Positive: the sequential scale runs from the light to the dark end.</summary>
    [Fact]
    public void Sequential_Ends_AreTheLightAndTheDarkBlue()
    {
        Assert.Equal("#eff3ff", HeatColor.Sequential(0));
        Assert.Equal("#08519c", HeatColor.Sequential(1));
        Assert.Equal("#08519c", HeatColor.Sequential(7));
    }

    #endregion

    #region Month grid

    /// <summary>Positive: months become one row for each year, oldest first, with a gap where a month is missing.</summary>
    [Fact]
    public void Rows_Months_GroupByYear()
    {
        // Arrange
        MarketAnalysisPointDto[] points = [new("2017-12", 5), new("2016-06", 1), new("2016-07", 2, true), new("2017-01", 3)];

        // Act
        IReadOnlyList<MonthRow> rows = MonthGrid.Rows(points);

        // Assert
        Assert.Equal([2016, 2017], rows.Select(row => row.Year));
        Assert.Equal(1, rows[0].Values[5]);
        Assert.Equal(2, rows[0].Values[6]);
        Assert.True(rows[0].SmallSamples[6]);
        Assert.Null(rows[0].Values[0]);
        Assert.Equal(5, rows[1].Values[11]);
        Assert.Equal(3, MonthGrid.YearCount([.. points, new("2019-01", 1)]));
    }

    /// <summary>Negative: labels that are not months are refused.</summary>
    [Theory]
    [InlineData("2018-13")]
    [InlineData("2018-W26")]
    [InlineData("18-07")]
    [InlineData("Online TA")]
    [InlineData("")]
    public void TryParse_NotAMonth_IsRefused(string label)
    {
        Assert.False(MonthGrid.TryParse(label, out _, out _));
    }

    /// <summary>Positive: a month label is read.</summary>
    [Fact]
    public void TryParse_Month_IsRead()
    {
        Assert.True(MonthGrid.TryParse("2018-07", out int year, out int month));
        Assert.Equal((2018, 7), (year, month));
    }

    #endregion
}
