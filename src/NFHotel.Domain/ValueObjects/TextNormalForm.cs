using System.Diagnostics.CodeAnalysis;

namespace NFHotel.Domain.ValueObjects;

/// <summary>
/// The normal form of a text that a person typed (MIL-086, ADR-0050). Skeleton of the tests: the text is kept as it is.
/// </summary>
public static class TextNormalForm
{
    /// <summary>Brings a text to its normal form.</summary>
    /// <param name="value">The text, or <see langword="null"/>.</param>
    /// <returns>The text.</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? Normalize(string? value) => value;

    /// <summary>Brings an optional text to its normal form.</summary>
    /// <param name="value">The text, or <see langword="null"/>.</param>
    /// <returns>The text.</returns>
    public static string? NormalizeOptional(string? value) => value;
}
