using NFHotel.Application.Dto.Benchmark;

namespace NFHotel.Application.Tests.Dto;

/// <summary>The shape of the name of an Ollama model, the rule of the benchmark service (ADR-0005 of that service).</summary>
public sealed class ModelNamesTests
{
    /// <summary>Positive: names with a path, a tag, dots, dashes and underscores are valid.</summary>
    [Theory]
    [InlineData("phi4-mini")]
    [InlineData("llama3.2:3b")]
    [InlineData("phi3.5:latest")]
    [InlineData("library/gemma2:9b")]
    [InlineData("hf.co/user/model:Q4_K_M")]
    [InlineData("a")]
    public void IsValid_ValidName_IsTrue(string name) => Assert.True(ModelNames.IsValid(name));

    /// <summary>Negative: anything that could carry a path, a command or a second tag is invalid.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a b")]
    [InlineData("../etc/passwd")]
    [InlineData("a;b")]
    [InlineData("a/b/c/d")]
    [InlineData("a//b")]
    [InlineData("-start")]
    [InlineData("name:")]
    [InlineData(":tag")]
    [InlineData("a:b:c")]
    public void IsValid_InvalidName_IsFalse(string? name) => Assert.False(ModelNames.IsValid(name));

    /// <summary>Boundary: 100 characters are valid and 101 are not.</summary>
    [Fact]
    public void IsValid_Length_AcceptsTheLimitAndRefusesOneMore()
    {
        // Arrange, act and assert
        Assert.True(ModelNames.IsValid(new string('x', ModelNames.MaxLength)));
        Assert.False(ModelNames.IsValid(new string('x', ModelNames.MaxLength + 1)));
    }
}
