using System.ComponentModel.DataAnnotations;

namespace NFHotel.Domain.Attributes;

/// <summary>
/// The format of a phone number (MIL-086 decision 2, ADR-0050): an optional plus first, then the digits 0 to 9 with
/// spaces, hyphens and round brackets between them; 7 to 15 digits (ITU-T E.164 has at most 15); after the plus it
/// starts with a digit or an opening bracket and ends with a digit or a closing bracket. The length is the rule of
/// <see cref="StringLengthAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class PhoneNumberAttribute : ValidationAttribute
{
    #region Fields

    /// <summary>The fewest digits of a phone number.</summary>
    public const int MinDigits = 7;

    /// <summary>The most digits of a phone number (ITU-T E.164).</summary>
    public const int MaxDigits = 15;

    #endregion Fields

    #region Public methods

    /// <summary>Tells whether a text is a phone number.</summary>
    /// <param name="value">The text, in normal form.</param>
    /// <returns><see langword="true"/> when the text has the allowed shape and number of digits.</returns>
    public static bool Accepts(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        string body = value[0] == '+' ? value[1..] : value;
        if (body.Length == 0
            || !(char.IsAsciiDigit(body[0]) || body[0] == '(')
            || !(char.IsAsciiDigit(body[^1]) || body[^1] == ')'))
        {
            return false;
        }

        int digits = 0;
        foreach (char character in body)
        {
            if (char.IsAsciiDigit(character))
            {
                digits++;
            }
            else if (character is not (' ' or '-' or '(' or ')'))
            {
                return false;
            }
        }

        return digits is >= MinDigits and <= MaxDigits;
    }

    /// <inheritdoc/>
    public override bool IsValid(object? value) => value is null || (value is string text && Accepts(text));

    #endregion Public methods
}
