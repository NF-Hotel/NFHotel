using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;

namespace NFHotel.Domain.Attributes;

/// <summary>
/// The characters of a first or last name (MIL-086 decision 2, ADR-0050): letters and marks of any script (Unicode
/// categories L and M), the space U+0020, the apostrophes U+0027 and U+2019, the hyphen-minus and the full stop, with
/// at least one letter. The length is the rule of <see cref="StringLengthAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class PersonNameAttribute : ValidationAttribute
{
    #region Public methods

    /// <summary>Tells whether a text is made of the characters of a name.</summary>
    /// <param name="value">The text, in normal form.</param>
    /// <returns><see langword="true"/> when every character is allowed and one is a letter.</returns>
    public static bool Accepts(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        bool hasLetter = false;

        // A lone surrogate comes out as U+FFFD, a symbol, so it is refused here.
        foreach (Rune rune in value.EnumerateRunes())
        {
            if (IsLetter(rune))
            {
                hasLetter = true;
            }
            else if (!IsMark(rune) && !IsAllowedSign(rune))
            {
                return false;
            }
        }

        return hasLetter;
    }

    /// <inheritdoc/>
    public override bool IsValid(object? value) => value is null || (value is string text && Accepts(text));

    #endregion Public methods

    #region Private methods

    private static bool IsLetter(Rune rune) => Rune.GetUnicodeCategory(rune) is
        UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or UnicodeCategory.TitlecaseLetter
        or UnicodeCategory.ModifierLetter or UnicodeCategory.OtherLetter;

    private static bool IsMark(Rune rune) => Rune.GetUnicodeCategory(rune) is
        UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark;

    private static bool IsAllowedSign(Rune rune) => rune.Value is ' ' or '\'' or '\u2019' or '-' or '.';

    #endregion Private methods
}
