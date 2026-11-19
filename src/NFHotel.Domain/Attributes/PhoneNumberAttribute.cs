using System.ComponentModel.DataAnnotations;

namespace NFHotel.Domain.Attributes;

/// <summary>
/// The format of a phone number (MIL-086, ADR-0050). Skeleton of the tests: no value is refused yet.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class PhoneNumberAttribute : ValidationAttribute
{
    /// <summary>Tells whether a text is a phone number.</summary>
    /// <param name="value">The text, in normal form.</param>
    /// <returns><see langword="true"/>.</returns>
    public static bool Accepts(string? value) => true;

    /// <inheritdoc/>
    public override bool IsValid(object? value) => true;
}
