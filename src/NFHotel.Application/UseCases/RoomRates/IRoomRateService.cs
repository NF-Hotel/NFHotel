using NFHotel.Application.Dto.RoomRates;

namespace NFHotel.Application.UseCases.RoomRates;

/// <summary>
/// Reads and sets the nightly base rate per room size (MIL-055). The API implements it with the repository, and the web
/// application with a client over HTTP.
/// </summary>
public interface IRoomRateService
{
    /// <summary>Lists the rates that are set.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The rates, ordered by room size.</returns>
    Task<Result<IReadOnlyList<RoomRateDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the rate of a room size; an earlier rate of that size is replaced. The staff member who sets it is the
    /// caller of the use case, and the change is written to the audit trail with the old and the new price.
    /// </summary>
    /// <param name="size">The room size: <c>single</c>, <c>double</c> or <c>suite</c>, in any letter case.</param>
    /// <param name="request">The new nightly price.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The rate after the change, or the codes that say what is wrong.</returns>
    Task<Result<RoomRateDto>> SetAsync(string size, SetRoomRateRequest request, CancellationToken cancellationToken = default);
}
