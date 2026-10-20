using NFHotel.Web.Features.Admin.Benchmark;

namespace NFHotel.Web.Tests;

/// <summary>Reading the parts of the answer of a model to the hotel cancellation analysis, for display.</summary>
public sealed class HotelAnswerTests
{
    private const string Json = """
        {
          "summary": "About a third of the bookings were cancelled.",
          "overall_cancellation": { "rate": 0.37, "interpretation": "Overall 37%." },
          "direct_customers": { "rate": 0.3, "interpretation": "Direct 30%." },
          "agent_bookings": { "rate": 0.4, "interpretation": "Agents 40%." },
          "key_findings": ["Agents cancel more.", "Deposits matter."],
          "limitations": ["Association is not cause."]
        }
        """;

    /// <summary>Positive: a valid answer gives every part.</summary>
    [Fact]
    public void TryParse_ValidAnswer_ReadsEveryPart()
    {
        // Arrange and act
        var answer = HotelAnswer.TryParse(Json);

        // Assert
        Assert.NotNull(answer);
        Assert.Equal("About a third of the bookings were cancelled.", answer.Summary);
        Assert.Equal((0.37, 0.3, 0.4), (answer.Overall.Rate, answer.Direct.Rate, answer.Agent.Rate));
        Assert.Equal("Agents 40%.", answer.Agent.Interpretation);
        Assert.Equal(["Agents cancel more.", "Deposits matter."], answer.KeyFindings);
        Assert.Equal(["Association is not cause."], answer.Limitations);
    }

    /// <summary>Positive: an answer inside a code fence is read.</summary>
    [Fact]
    public void TryParse_AnswerInACodeFence_ReadsIt()
    {
        // Arrange and act
        var answer = HotelAnswer.TryParse("```json\n" + Json + "\n```");

        // Assert
        Assert.Equal(0.37, answer!.Overall.Rate);
    }

    /// <summary>Boundary: a rate written as text has no number, and the other parts are kept.</summary>
    [Fact]
    public void TryParse_RateAsText_HasNoNumberAndKeepsTheRest()
    {
        // Arrange and act
        var answer = HotelAnswer.TryParse(Json.Replace("0.37", "\"0.37\"", StringComparison.Ordinal));

        // Assert
        Assert.Null(answer!.Overall.Rate);
        Assert.Equal(0.3, answer.Direct.Rate);
    }

    /// <summary>Boundary: a missing part is empty, not an error.</summary>
    [Fact]
    public void TryParse_MissingParts_AreEmpty()
    {
        // Arrange and act
        var answer = HotelAnswer.TryParse("{}");

        // Assert
        Assert.Equal(string.Empty, answer!.Summary);
        Assert.Null(answer.Direct.Rate);
        Assert.Empty(answer.KeyFindings);
    }

    /// <summary>Negative: text that is not a JSON object cannot be read.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Sorry, I cannot do that.")]
    [InlineData("[1, 2]")]
    [InlineData("{\"summary\": ")]
    public void TryParse_NotAJsonObject_ReturnsNull(string? text) => Assert.Null(HotelAnswer.TryParse(text));
}
