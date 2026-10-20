using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;

namespace NFHotel.Domain.Tests.MarketAnalysis;

/// <summary>The stored market analysis run and the rules of its header (MIL-026).</summary>
public sealed class MarketAnalysisRunTests
{
    private static readonly Guid RunId = Guid.Parse("5b3f0e1a-0000-4000-8000-0000000000a1");
    private static readonly DateTimeOffset Start = new(2026, 10, 19, 10, 0, 0, TimeSpan.FromHours(2));

    /// <summary>Positive: a valid header is stored with its times in UTC.</summary>
    [Fact]
    public void Create_ValidHeader_StoresItWithUtcTimes()
    {
        // Act
        var run = MarketAnalysisRun.Create(RunId, 1, Start, Start.AddMinutes(4), "ollama", "phi3.5:latest", "{}");

        // Assert
        Assert.Equal((RunId, 1, "ollama", "phi3.5:latest", "{}"), (run.RunId, run.SchemaVersion, run.Provider, run.Model, run.ContentJson));
        Assert.Equal(TimeSpan.Zero, run.StartedAt.Offset);
        Assert.Equal(Start.ToUniversalTime(), run.StartedAt);
    }

    /// <summary>Boundary: a run that took no time, and a run without an AI model, are valid.</summary>
    [Fact]
    public void Create_ZeroDurationAndNoModel_IsValid()
    {
        // Act
        var run = MarketAnalysisRun.Create(RunId, 1, Start, Start, null, null, "{}");

        // Assert
        Assert.Null(run.Model);
        Assert.Equal(run.StartedAt, run.FinishedAt);
    }

    /// <summary>Negative: the empty run id, an end before the start and an empty content are refused together.</summary>
    [Fact]
    public void Create_SeveralDefects_ThrowsAllTheCodes()
    {
        // Act
        var exception = Assert.Throws<DomainException>(
            () => MarketAnalysisRun.Create(Guid.Empty, 1, Start, Start.AddSeconds(-1), null, null, string.Empty));

        // Assert
        Assert.Equivalent(
            new[] { MarketAnalysisErrors.RunIdRequired, MarketAnalysisErrors.EndBeforeStart, MarketAnalysisErrors.ContentRequired },
            exception.Codes);
    }

    /// <summary>Boundary: a model name of 200 characters is valid and one of 201 is refused.</summary>
    [Fact]
    public void Create_ModelNameLength_AllowsTheLimitAndRefusesOneMore()
    {
        // Act
        var atLimit = MarketAnalysisRun.Create(RunId, 1, Start, Start, null, new string('m', MarketAnalysisRun.MaxTextLength), "{}");
        var exception = Assert.Throws<DomainException>(
            () => MarketAnalysisRun.Create(RunId, 1, Start, Start, null, new string('m', MarketAnalysisRun.MaxTextLength + 1), "{}"));

        // Assert
        Assert.Equal(MarketAnalysisRun.MaxTextLength, atLimit.Model!.Length);
        Assert.Contains(MarketAnalysisErrors.ModelInvalid, exception.Codes);
    }
}
