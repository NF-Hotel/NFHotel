namespace NFHotel.Domain.ValueObjects;

/// <summary>
/// The countries that a guest chooses from (MIL-086, ADR-0050). Skeleton of the tests: the list is empty.
/// </summary>
public static class Countries
{
    /// <summary>The countries, sorted by name.</summary>
    public static IReadOnlyList<IsoCountry> All { get; } = Array.Empty<IsoCountry>();
}
