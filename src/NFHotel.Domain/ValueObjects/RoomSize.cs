namespace NFHotel.Domain.ValueObjects;

/// <summary>
/// Værelsestype. Erstatter det gamle fritekstfelt <c>Room.RoomSize</c>.
/// </summary>
/// <remarks>
/// Ordinalværdierne er nye — der er ingen eksisterende int-data at bevare, kun tekst
/// (<c>'Single'</c>/<c>'Double'</c>/<c>'Suite'</c>) som infrastrukturlaget skal mappe
/// ved migreringen. Bliver senere ophæng for en <c>RoomType</c> med pris.
/// </remarks>
public enum RoomSize
{
    /// <summary>Enkeltværelse.</summary>
    Single = 0,

    /// <summary>Dobbeltværelse.</summary>
    Double = 1,

    /// <summary>Suite.</summary>
    Suite = 2
}