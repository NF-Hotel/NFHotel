using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Repositories;

/// <summary>Storage of the nightly base rate per room size (MIL-055).</summary>
public interface IRoomRateRepository : IRepository<RoomRate, int>
{
    /// <summary>Finds the rate of a room size.</summary>
    /// <param name="size">The room size.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The rate, or <c>null</c> when none is set.</returns>
    Task<RoomRate?> FindBySizeAsync(RoomSize size, CancellationToken cancellationToken = default);

    /// <summary>Lists every rate that is set, ordered by room size.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The rates.</returns>
    Task<IReadOnlyList<RoomRate>> ListAsync(CancellationToken cancellationToken = default);
}
