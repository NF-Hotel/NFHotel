using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace NFHotel.Domain.ValueObjects;

/// <summary>
/// The normal form of a text that a person typed (MIL-086 decision 4, ADR-0050): white space trimmed at both ends and
/// Unicode normal form C, so that "José" typed as two code points and as one is the same text before any rule runs.
/// </summary>
/// <remarks>
/// A text that the runtime cannot normalise, one with a lone surrogate or the noncharacter U+FFFE (it throws
/// <see cref="ArgumentException"/> for invalid Unicode), is only trimmed and left for the rule of its field to refuse.
/// Nothing is stripped or rewritten beyond this (MIL-086 decision 5).
/// </remarks>
public static class TextNormalForm
{
    #region Public methods

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

        string trimmed = value.Trim();

        try
        {
            return trimmed.Normalize(NormalizationForm.FormC);
        }
        catch (ArgumentException)
        {
            // Invalid Unicode for the runtime: the rule of the field refuses the text as it is.
            return trimmed;
        }
    }

    /// <summary>Brings an optional text to its normal form, where nothing but white space means no value.</summary>
    /// <param name="value">The text, or <see langword="null"/>.</param>
    /// <returns>The normal form, or <see langword="null"/> when nothing is left.</returns>
    public static string? NormalizeOptional(string? value)
    {
        string? normalized = Normalize(value);

        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    #endregion Public methods
}
