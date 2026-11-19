using System.ComponentModel.DataAnnotations;

namespace NFHotel.Domain.Attributes;

/// <summary>
/// The format of a first or a last name (MIL-086, ADR-0050). Skeleton of the tests: no value is refused yet.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class PersonNameAttribute : ValidationAttribute
{
    /// <summary>Tells whether a text is a name of a person.</summary>
    /// <param name="value">The text, in normal form.</param>
    /// <returns><see langword="true"/>.</returns>
    public static bool Accepts(string? value) => true;

    /// <inheritdoc/>
    public override bool IsValid(object? value) => true;
}
