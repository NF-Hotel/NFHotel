using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Rooms;

/// <summary>
/// Valgmulighederne i rumfilteret (BR-75).
/// </summary>
/// <remarks>
/// Kun etager kommer fra data. Størrelse, status og rengøringsstand kommer fra enums, hvilket
/// automatisk lukker BR-126, hvor det gamle systems XAML-dropdown og enum var uenige.
/// </remarks>
/// <param name="Floors">Etager der faktisk findes rum på, stigende.</param>
/// <param name="Sizes">Alle definerede værelsestyper.</param>
/// <param name="Statuses">Alle definerede bookbarhedsstatusser.</param>
/// <param name="HousekeepingStatuses">Alle definerede rengørings- og servicestande.</param>
public sealed record RoomFilterOptionsDto(
    IReadOnlyList<int> Floors,
    IReadOnlyList<RoomSize> Sizes,
    IReadOnlyList<RoomStatus> Statuses,
    IReadOnlyList<HousekeepingStatus> HousekeepingStatuses);