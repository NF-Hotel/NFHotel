namespace NFHotel.Domain.ValueObjects;

/// <summary>A country of ISO 3166-1, as a guest chooses it (MIL-086, ADR-0050).</summary>
/// <param name="Alpha2Code">The two-letter code of ISO 3166-1.</param>
/// <param name="Name">The English short name of ISO 3166-1, which a guest stores.</param>
public sealed record IsoCountry(string Alpha2Code, string Name);
