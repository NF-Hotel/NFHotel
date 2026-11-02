using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.RoomRates;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.RoomRates;

namespace NFHotel.ApiClient;

/// <summary>
/// Reads and sets the nightly base rate per room size through the API for the web application (MIL-051). The API takes
/// the name of the staff member from the staff token.
/// </summary>
public sealed class ApiRoomRateService : IRoomRateService
{
    private readonly ApiGateway _api;

    /// <summary>Creates the client.</summary>
    /// <param name="api">The gateway that carries the tokens.</param>
    public ApiRoomRateService(ApiGateway api)
    {
        ArgumentNullException.ThrowIfNull(api);
        _api = api;
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<RoomRateDto>>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _api.GetAsync<IReadOnlyList<RoomRateDto>>(ApiRoutes.RoomRates, cancellationToken);

    /// <inheritdoc />
    public Task<Result<RoomRateDto>> SetAsync(string size, SetRoomRateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return _api.PutAsync<RoomRateDto>($"{ApiRoutes.RoomRates}/{Uri.EscapeDataString(size ?? string.Empty)}", request, cancellationToken);
    }
}
