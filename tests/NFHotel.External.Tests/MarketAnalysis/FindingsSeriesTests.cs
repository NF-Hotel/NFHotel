using System.Text.Json;

using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.External.MarketAnalysis;

namespace NFHotel.External.Tests.MarketAnalysis;

/// <summary>Finding the charts in the free-form findings of an analysis of the markets API (MIL-026).</summary>
public sealed class FindingsSeriesTests
{
    #region Group figures

    /// <summary>Positive: a list of group figures with a numerator and a denominator is a series of rates in percent.</summary>
    [Fact]
    public void Extract_RateGroups_AreAPercentSeries()
    {
        // Act
        var series = Extract("""{"by_deposit_type":[{"group":"No Deposit","numerator":9000,"denominator":32700,"small_sample":false},{"group":"Refundable","numerator":3,"denominator":60,"small_sample":true}]}""");

        // Assert
        var found = Assert.Single(series);
        Assert.Equal(("by_deposit_type", "By deposit type", "%"), (found.Id, found.Title, found.Unit));
        Assert.Equal([("No Deposit", 27.5), ("Refundable", 5.0)], found.Points.Select(point => (point.Label, point.Value)));
    }

    /// <summary>Positive: a value that is a decimal string, as money is in the markets API, is read as a number.</summary>
    [Fact]
    public void Extract_ValueAsString_IsReadAsANumber()
    {
        // Act
        var series = Extract("""{"mean_price":[{"group":"A","value":"78.40"},{"group":"C","value":104.2}]}""");

        // Assert
        Assert.Equal([("A", 78.4), ("C", 104.2)], Assert.Single(series).Points.Select(point => (point.Label, point.Value)));
    }

    /// <summary>Boundary: a count is a series of bookings, and a group that is a number gets that number as its label.</summary>
    [Fact]
    public void Extract_CountAndNumericGroup_UseBookingsAndTheNumberAsLabel()
    {
        // Act
        var series = Extract("""{"per_month":[{"group":1,"count":10},{"group":2,"count":20}]}""");

        // Assert
        var found = Assert.Single(series);
        Assert.Equal("bookings", found.Unit);
        Assert.Equal(["1", "2"], found.Points.Select(point => point.Label));
    }

    /// <summary>Boundary: a group with a zero denominator is left out, and nothing else is.</summary>
    [Fact]
    public void Extract_ZeroDenominator_LeavesThatPointOut()
    {
        // Act
        var series = Extract("""{"x":[{"group":"a","numerator":0,"denominator":0},{"group":"b","numerator":1,"denominator":4}]}""");

        // Assert
        Assert.Equal([("b", 25.0)], Assert.Single(series).Points.Select(point => (point.Label, point.Value)));
    }

    /// <summary>Boundary: more than 30 points are cut to 30 and more than eight series to eight, in the order found.</summary>
    [Fact]
    public void Extract_ManyPointsAndSeries_AreCut()
    {
        // Arrange
        var points = string.Join(",", Enumerable.Range(1, 40).Select(i => $"{{\"group\":\"g{i}\",\"count\":{i}}}"));
        var findings = "{" + string.Join(",", Enumerable.Range(1, 10).Select(i => $"\"s{i}\":[{points}]")) + "}";

        // Act
        var series = Extract(findings);

        // Assert
        Assert.Equal(FindingsSeries.MaxSeries, series.Count);
        Assert.All(series, found => Assert.Equal(FindingsSeries.MaxPoints, found.Points.Count));
        Assert.Equal("g1", series[0].Points[0].Label);
    }

    /// <summary>Positive: a monthly series of more than 30 points keeps its latest 30, and its title says how many of how many.</summary>
    [Theory]
    [InlineData(40)]
    [InlineData(103)]
    public void Extract_LongMonthlySeries_KeepsTheLatestPointsAndSaysSo(int months)
    {
        // Arrange
        var labels = Enumerable.Range(0, months).Select(i => new DateTime(2018, 7, 1).AddMonths(i).ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture)).ToList();
        var points = string.Join(",", labels.Select((label, i) => $"{{\"group\":\"{label}\",\"count\":{i}}}"));

        // Act
        var found = Assert.Single(Extract("{\"occupancy\":{\"monthly\":[" + points + "]}}"));

        // Assert
        Assert.Equal("occupancy.monthly", found.Id);
        Assert.Equal($"Occupancy - monthly (latest 30 of {months} months)", found.Title);
        Assert.Equal(labels.TakeLast(30), found.Points.Select(point => point.Label));
        Assert.Equal(months - 1, found.Points[^1].Value);
    }

    /// <summary>Boundary: a monthly series of exactly 30 points is untouched, title included.</summary>
    [Fact]
    public void Extract_MonthlySeriesOf30_IsUntouched()
    {
        // Arrange
        var points = string.Join(",", Enumerable.Range(1, 30).Select(i => $"{{\"group\":\"2020-{(i % 12) + 1:00}\",\"count\":{i}}}"));

        // Act
        var found = Assert.Single(Extract("{\"monthly\":[" + points + "]}"));

        // Assert
        Assert.Equal("Monthly", found.Title);
        Assert.Equal(30, found.Points.Count);
    }

    /// <summary>Negative: a weekly series of 100 points is too dense and is left out.</summary>
    [Fact]
    public void Extract_DenseWeeklySeries_IsLeftOut()
    {
        // Arrange
        var points = string.Join(",", Enumerable.Range(1, 100).Select(i => $"{{\"group\":\"{2018 + i / 52}-W{i % 52 + 1:00}\",\"count\":{i}}}"));

        // Act and assert
        Assert.Empty(Extract("{\"iso_week\":[" + points + "]}"));
    }

    /// <summary>Negative: a category series of 70 points is left out, and one of 40 keeps its first 30 with its title unchanged.</summary>
    [Fact]
    public void Extract_LongCategorySeries_KeepsTodaysRule()
    {
        // Arrange
        string Series(int n) => "[" + string.Join(",", Enumerable.Range(1, n).Select(i => $"{{\"group\":\"c{i}\",\"count\":{i}}}")) + "]";

        // Act
        var dropped = Extract("{\"country\":" + Series(70) + "}");
        var cut = Assert.Single(Extract("{\"country\":" + Series(40) + "}"));

        // Assert
        Assert.Empty(dropped);
        Assert.Equal(("country", "Country"), (cut.Id, cut.Title));
        Assert.Equal(Enumerable.Range(1, 30).Select(i => $"c{i}"), cut.Points.Select(point => point.Label));
    }

    /// <summary>Positive: the group figures of the real findings are wrapped as <c>figure</c> beside other fields, and are read from it.</summary>
    [Fact]
    public void Extract_WrappedFigures_AreReadFromTheFigure()
    {
        // Act
        var series = Extract("""{"splits":{"by_deposit_type":{"status":"available","groups":[{"figure":{"group":"No Deposit","numerator":9450,"denominator":38199,"small_sample":false}},{"figure":{"group":"Refundable","numerator":22,"denominator":142,"small_sample":false},"partial_period":false}]}}}""");

        // Assert
        var found = Assert.Single(series);
        Assert.Equal(("splits.by_deposit_type.groups", "By deposit type"), (found.Id, found.Title));
        Assert.Equal(["No Deposit", "Refundable"], found.Points.Select(point => point.Label));
        Assert.Equal(24.7, found.Points[0].Value);
    }

    /// <summary>Boundary: a series of more than 60 points, such as the weekly ones, is left out, and the overview series come first.</summary>
    [Fact]
    public void Extract_DenseSeries_IsLeftOutAndTheShallowOnesComeFirst()
    {
        // Arrange
        var dense = string.Join(",", Enumerable.Range(1, 61).Select(i => $"{{\"group\":\"w{i}\",\"count\":{i}}}"));
        var findings = "{\"a\":{\"b\":{\"periods\":[" + dense + "]}},\"top\":[{\"group\":\"x\",\"count\":1}],\"deep\":{\"deeper\":{\"deepest\":[{\"group\":\"y\",\"count\":2}]}}}";

        // Act
        var series = Extract(findings);

        // Assert
        Assert.Equal(["top", "deep.deeper.deepest"], series.Select(found => found.Id));
    }

    #endregion

    #region Shapes of the findings

    /// <summary>Positive: lists nested in objects and in lists are found, with the path as the id.</summary>
    [Fact]
    public void Extract_NestedFindings_FindsTheListWithItsPath()
    {
        // Act
        var series = Extract("""{"booking":{"by_segment":[{"group":"Direct","count":5}]},"windows":[{"size":1,"groups":[{"group":"in","count":2}]}]}""");

        // Assert
        Assert.Equal(["booking.by_segment", "windows.0.groups"], series.Select(found => found.Id));
        Assert.Equal("Booking - by segment", series[0].Title);
        Assert.Equal("Windows", series[1].Title);
    }

    /// <summary>Negative: findings without group figures give no chart, and a list of other things is not a series.</summary>
    [Theory]
    [InlineData("{}")]
    [InlineData("""{"count":5,"names":["a","b"],"rows":[{"x":1}]}""")]
    [InlineData("""{"group_only":[{"group":"a"}]}""")]
    public void Extract_NoGroupFigures_GivesNoChart(string findings)
    {
        // Act and assert
        Assert.Empty(Extract(findings));
    }

    /// <summary>Positive: two group figures that sit directly in the findings are one series of rates, first; one figure alone is not.</summary>
    [Fact]
    public void Extract_TwoFiguresInTheRoot_AreOneSeries()
    {
        // Act
        var pair = Extract("""{"monthly":[{"group":"2018-07","numerator":1,"denominator":4}],"holiday_days":{"group":"holiday_days","numerator":1,"denominator":4,"small_sample":true},"other_days":{"group":"other_days","numerator":3,"denominator":4}}""");
        var single = Extract("""{"overall":{"group":"overall","numerator":1,"denominator":2}}""");

        // Assert
        Assert.Equal(["holiday_days+other_days", "monthly"], pair.Select(found => found.Id));
        Assert.Equal([25.0, 75.0], pair[0].Points.Select(point => point.Value));
        Assert.True(pair[0].Points[0].SmallSample);
        Assert.Empty(single);
    }

    /// <summary>Boundary: a part of the findings that is marked unavailable gives no chart, and its sibling does.</summary>
    [Fact]
    public void Extract_UnavailablePart_IsNotDrawn()
    {
        // Act
        var series = Extract("""{"estimated":{"status":"unavailable","groups":[{"group":"a","count":1}]},"stay":{"status":"available","groups":[{"group":"1","count":2}]}}""");

        // Assert
        Assert.Equal(["stay.groups"], series.Select(found => found.Id));
    }

    #endregion

    #region Helpers

    private static IReadOnlyList<MarketAnalysisSeriesDto> Extract(string json)
    {
        using var document = JsonDocument.Parse(json);
        return FindingsSeries.Extract(document.RootElement);
    }

    #endregion
}
