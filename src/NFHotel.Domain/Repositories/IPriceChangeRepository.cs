using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Repositories;

/// <summary>Storage of the price changes of the room sizes (MIL-055).</summary>
public interface IPriceChangeRepository : IRepository<PriceChange, int>
{
    /// <summary>
    /// Lists the changes of a room size whose declared period shares a day with a period, whatever their state. A caller
    /// that needs the days a change really counts for asks the change (<see cref="PriceChange.CountsWithin"/>).
    /// </summary>
    /// <param name="size">The room size.</param>
    /// <param name="from">The first day of the period.</param>
    /// <param name="to">The last day of the period.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The changes, ordered by first day and key.</returns>
    Task<IReadOnlyList<PriceChange>> ListForSizeAsync(
        RoomSize size,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    /// <summary>Lists every change, ordered by first day, then room size, then key.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The changes.</returns>
    Task<IReadOnlyList<PriceChange>> ListAsync(CancellationToken cancellationToken = default);
}
