using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;

namespace NFHotel.Domain.Tests.Benchmark;

/// <summary>The rules of the default AI model of a purpose (ADR-0014).</summary>
public sealed class AiDefaultTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 19, 10, 0, 0, TimeSpan.FromHours(2));
    private static readonly Guid RunId = Guid.Parse("5b3f0e1a-0000-4000-8000-000000000001");

    /// <summary>Positive: a valid default keeps its fields, trims the text and stores the time in UTC.</summary>
    [Fact]
    public void Create_ValidFields_KeepsThemTrimmedAndInUtc()
    {
        // Arrange and act
        var value = AiDefault.Create(AiDefault.Website, "  llama3.2:3b ", " manager ", At, RunId);

        // Assert
        Assert.Equal(("website", "llama3.2:3b", "manager"), (value.Purpose, value.Model, value.SetBy));
        Assert.Equal(TimeSpan.Zero, value.SetAt.Offset);
        Assert.Equal(At.UtcDateTime, value.SetAt.UtcDateTime);
        Assert.Equal(RunId, value.SourceRunId);
    }

    /// <summary>Boundary: the empty run id is no source.</summary>
    [Fact]
    public void Create_EmptyRunId_HasNoSource()
    {
        // Arrange and act
        var value = AiDefault.Create(AiDefault.MarketAnalysis, "phi3.5", "manager", At, Guid.Empty);

        // Assert
        Assert.Null(value.SourceRunId);
    }

    /// <summary>Negative: an unknown purpose is refused with its code.</summary>
    [Fact]
    public void Create_UnknownPurpose_ThrowsThePurposeCode()
    {
        // Arrange and act
        var exception = Assert.Throws<DomainException>(() => AiDefault.Create("billing", "m", "manager", At, null));

        // Assert
        Assert.Contains(AiDefaultErrors.PurposeInvalid, exception.Codes);
    }

    /// <summary>Negative: a blank or too long model is refused with its code.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankModel_ThrowsTheModelCode(string model)
    {
        // Arrange and act
        var exception = Assert.Throws<DomainException>(() => AiDefault.Create(AiDefault.Website, model, "manager", At, null));

        // Assert
        Assert.Contains(AiDefaultErrors.ModelInvalid, exception.Codes);
    }

    /// <summary>Boundary: a model of 100 characters is kept, one of 101 is refused.</summary>
    [Fact]
    public void Create_ModelLength_AcceptsTheLimitAndRefusesOneMore()
    {
        // Arrange and act
        var accepted = AiDefault.Create(AiDefault.Website, new string('m', AiDefault.MaxTextLength), "manager", At, null);
        var exception = Assert.Throws<DomainException>(() => AiDefault.Create(AiDefault.Website, new string('m', AiDefault.MaxTextLength + 1), "manager", At, null));

        // Assert
        Assert.Equal(AiDefault.MaxTextLength, accepted.Model.Length);
        Assert.Contains(AiDefaultErrors.ModelInvalid, exception.Codes);
    }

    /// <summary>Negative: a missing user name is refused with its code.</summary>
    [Fact]
    public void Create_MissingSetBy_ThrowsTheSetByCode()
    {
        // Arrange and act
        var exception = Assert.Throws<DomainException>(() => AiDefault.Create(AiDefault.Website, "m", string.Empty, At, null));

        // Assert
        Assert.Contains(AiDefaultErrors.SetByInvalid, exception.Codes);
    }

    /// <summary>State: a change replaces the model, the user, the time and the source, and keeps the purpose.</summary>
    [Fact]
    public void Change_ValidFields_ReplacesEverythingButThePurpose()
    {
        // Arrange
        var value = AiDefault.Create(AiDefault.Website, "m1", "manager", At, RunId);

        // Act
        value.Change("m2", "other", At.AddHours(1), null);

        // Assert
        Assert.Equal(("website", "m2", "other"), (value.Purpose, value.Model, value.SetBy));
        Assert.Equal(At.AddHours(1).UtcDateTime, value.SetAt.UtcDateTime);
        Assert.Null(value.SourceRunId);
    }

    /// <summary>State: a refused change leaves the default as it was.</summary>
    [Fact]
    public void Change_BlankModel_ThrowsAndKeepsTheOldModel()
    {
        // Arrange
        var value = AiDefault.Create(AiDefault.Website, "m1", "manager", At, RunId);

        // Act
        Assert.Throws<DomainException>(() => value.Change(" ", "other", At.AddHours(1), null));

        // Assert
        Assert.Equal(("m1", "manager", RunId), (value.Model, value.SetBy, value.SourceRunId));
    }
}
