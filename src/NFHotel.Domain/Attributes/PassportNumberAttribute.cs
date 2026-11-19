using System.ComponentModel.DataAnnotations;

namespace NFHotel.Domain.Attributes;

/// <summary>
/// The format of a passport number (MIL-086 decision 2, ADR-0050): ASCII letters and digits only, as the machine
/// readable zone of a travel document holds them (ICAO Doc 9303). The length is the rule of
/// <see cref="StringLengthAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class PassportNumberAttribute : ValidationAttribute
{
    #region Public methods

    /// <summary>Tells whether a text is made of ASCII letters and digits.</summary>
    /// <param name="value">The text, in normal form.</param>
    /// <returns><see langword="true"/> when the text is not empty and every character is an ASCII letter or digit.</returns>
    public static bool Accepts(string? value) => !string.IsNullOrEmpty(value) && value.All(char.IsAsciiLetterOrDigit);

    /// <inheritdoc/>
    public override bool IsValid(object? value) => value is null || (value is string text && Accepts(text));

    #endregion Public methods
}
