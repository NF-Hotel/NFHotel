namespace NFHotel.Domain.ValueObjects;

/// <summary>A country of ISO 3166-1: its alpha-2 code and its English short name (MIL-086, ADR-0050).</summary>
/// <param name="Alpha2Code">The two capital letters of the code, such as <c>DK</c>.</param>
/// <param name="Name">The English short name, such as <c>Denmark</c>, as a guest record stores it.</param>
public sealed record IsoCountry(string Alpha2Code, string Name);
