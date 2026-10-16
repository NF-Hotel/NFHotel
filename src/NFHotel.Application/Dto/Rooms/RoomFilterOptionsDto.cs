using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Rooms;

/// <summary>
/// Valgmulighederne i rumfilteret (BR-75).
/// </summary>
/// <remarks>
/// Only floors come from data. Size, status and cleaning state come from enums, which
/// automatically closes BR-126, where the old system's XAML dropdown and enum disagreed.
/// </remarks>
/// <param name="Floors">Floors that actually have rooms, ascending.</param>
/// <param name="Sizes">All defined room types.</param>
/// <param name="Statuses">All defined bookability statuses.</param>
/// <param name="HousekeepingStatuses">All defined cleaning and service states.</param>
public sealed record RoomFilterOptionsDto(
    IReadOnlyList<int> Floors,
    IReadOnlyList<RoomSize> Sizes,
    IReadOnlyList<RoomStatus> Statuses,
    IReadOnlyList<HousekeepingStatus> HousekeepingStatuses);