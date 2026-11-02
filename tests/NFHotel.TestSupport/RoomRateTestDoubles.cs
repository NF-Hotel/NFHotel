using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.TestSupport;

/// <summary>An in-memory repository of the room rates that finds a rate by its room size.</summary>
public sealed class InMemoryRoomRateRepository : InMemoryRepository<RoomRate>, IRoomRateRepository
{
    public Task<RoomRate?> FindBySizeAsync(RoomSize size, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(value => value.Size == size));

    public Task<IReadOnlyList<RoomRate>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RoomRate>>([.. Items.OrderBy(value => value.Size)]);
}
