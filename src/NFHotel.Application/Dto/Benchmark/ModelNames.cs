using System.Text.RegularExpressions;

namespace NFHotel.Application.Dto.Benchmark;

/// <summary>
/// The shape of the name of an Ollama model: up to three path parts and an optional tag, at most 100 characters. It is the
/// rule of the benchmark service (ADR-0005 of that service), kept here so that an obvious mistake does not cost a call.
/// </summary>
public static class ModelNames
{
    /// <summary>The longest model name.</summary>
    public const int MaxLength = 100;

    private static readonly Regex Shape = new(
        @"^[A-Za-z0-9][A-Za-z0-9._-]*(/[A-Za-z0-9][A-Za-z0-9._-]*){0,2}(:[A-Za-z0-9][A-Za-z0-9._-]*)?$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    /// <summary>Tells whether a text is shaped like the name of an Ollama model.</summary>
    /// <param name="name">The text, already trimmed.</param>
    /// <returns>True for a valid name.</returns>
    public static bool IsValid(string? name) =>
        !string.IsNullOrEmpty(name) && name.Length <= MaxLength && Shape.IsMatch(name);
}
