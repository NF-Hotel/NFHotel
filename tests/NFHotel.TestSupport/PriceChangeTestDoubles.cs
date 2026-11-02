using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.TestSupport;

/// <summary>An in-memory repository of the price changes that finds the changes of a size whose declared period touches a period.</summary>
public sealed class InMemoryPriceChangeRepository : InMemoryRepository<PriceChange>, IPriceChangeRepository
{
    public Task<IReadOnlyList<PriceChange>> ListForSizeAsync(
        RoomSize size,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PriceChange>>(
        [
            .. Items
                .Where(value => value.Size == size && value.FirstDay <= to && value.LastDay >= from)
                .OrderBy(value => value.FirstDay)
                .ThenBy(value => value.Id),
        ]);

    public Task<IReadOnlyList<PriceChange>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PriceChange>>(
        [
            .. Items
                .OrderBy(value => value.FirstDay)
                .ThenBy(value => value.Size)
                .ThenBy(value => value.Id),
        ]);
}
