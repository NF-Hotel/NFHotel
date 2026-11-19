using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace NFHotel.Domain.ValueObjects;

/// <summary>
/// The normal form of a text that a person typed (MIL-086 decision 4, ADR-0050): white space trimmed at both ends and
/// Unicode normal form C, so that "José" typed as two code points and as one is the same text before any rule runs.
/// </summary>
/// <remarks>
/// A text with a lone surrogate cannot be normalised (the runtime throws), so it is only trimmed and left for the rule
/// of its field to refuse. Nothing is stripped or rewritten beyond this (MIL-086 decision 5).
/// </remarks>
public static class TextNormalForm
{
    /// <summary>Trims a text and brings it to Unicode normal form C.</summary>
    /// <param name="value">The text, or <see langword="null"/>.</param>
    /// <returns>The normal form, <see cref="string.Empty"/> for white space only, or <see langword="null"/> for none.</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static string? Normalize(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();

        return HasLoneSurrogate(trimmed) ? trimmed : trimmed.Normalize(NormalizationForm.FormC);
    }

    /// <summary>Brings an optional text to its normal form, where nothing but white space means no value.</summary>
    /// <param name="value">The text, or <see langword="null"/>.</param>
    /// <returns>The normal form, or <see langword="null"/> when nothing is left.</returns>
    public static string? NormalizeOptional(string? value)
    {
        var normalized = Normalize(value);

        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static bool HasLoneSurrogate(string text)
    {
        for (var index = 0; index < text.Length; index++)
        {
            if (char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
            {
                index++;
            }
            else if (char.IsSurrogate(text[index]))
            {
                return true;
            }
        }

        return false;
    }
}
