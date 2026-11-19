using System.ComponentModel.DataAnnotations;

namespace NFHotel.Domain.Attributes;

/// <summary>
/// The format of an email address (MIL-086, ADR-0050). Skeleton of the tests: no value is refused yet.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class EmailFormatAttribute : ValidationAttribute
{
    /// <summary>Tells whether a text is one plain email address.</summary>
    /// <param name="value">The text, in normal form.</param>
    /// <returns><see langword="true"/>.</returns>
    public static bool Accepts(string? value) => true;

    /// <inheritdoc/>
    public override bool IsValid(object? value) => true;
}
