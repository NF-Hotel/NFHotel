using System.ComponentModel.DataAnnotations;

using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Attributes;

/// <summary>
/// A country is one of the names of <see cref="Countries"/>, exactly (MIL-086 decision 2, ADR-0050): the English
/// short name of ISO 3166-1, with its letter case and its accents.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class CountryNameAttribute : ValidationAttribute
{
    #region Public methods

    /// <summary>Tells whether a text is the name of a country of the list.</summary>
    /// <param name="value">The text, in normal form.</param>
    /// <returns><see langword="true"/> when the list holds the name as it is written.</returns>
    public static bool Accepts(string? value) => value is not null && Countries.IsName(value);

    /// <inheritdoc/>
    public override bool IsValid(object? value) => value is null || (value is string text && Accepts(text));

    #endregion Public methods
}
