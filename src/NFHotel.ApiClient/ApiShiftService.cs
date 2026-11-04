using System.Globalization;

using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Shifts;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Shifts;

namespace NFHotel.ApiClient;

/// <summary><see cref="IShiftService"/> over HTTP: the API holds the shifts and checks the role of the caller (MIL-060).</summary>
public sealed class ApiShiftService : IShiftService
{
    private readonly ApiGateway _api;

    /// <summary>Creates the service.</summary>
    /// <param name="api">The gateway to the API.</param>
    public ApiShiftService(ApiGateway api)
    {
        ArgumentNullException.ThrowIfNull(api);
        _api = api;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ShiftDto>>> ListAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default) =>
        await _api.GetAsync<IReadOnlyList<ShiftDto>>(
            $"{ApiRoutes.Shifts}?from={Moment(from)}&to={Moment(to)}",
            cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Result<ShiftDto>> CreateAsync(SaveShiftRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await _api.PostAsync<ShiftDto>(ApiRoutes.Shifts, request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<ShiftDto>> UpdateAsync(int id, SaveShiftRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ArgumentNullException.ThrowIfNull(request);

        return await _api.PutAsync<ShiftDto>($"{ApiRoutes.Shifts}/{id}", request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        return await _api.DeleteAsync($"{ApiRoutes.Shifts}/{id}", cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ShiftTradeDto>>> ListTradesAsync(CancellationToken cancellationToken = default) =>
        await _api.GetAsync<IReadOnlyList<ShiftTradeDto>>(ApiRoutes.ShiftTrades, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Result<ShiftTradeDto>> OfferTradeAsync(OfferShiftTradeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await _api.PostAsync<ShiftTradeDto>(ApiRoutes.ShiftTrades, request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Result<ShiftTradeDto>> TakeTradeAsync(int id, CancellationToken cancellationToken = default) => StepAsync(id, "take", cancellationToken);

    /// <inheritdoc />
    public Task<Result<ShiftTradeDto>> AcceptTradeAsync(int id, CancellationToken cancellationToken = default) => StepAsync(id, "accept", cancellationToken);

    /// <inheritdoc />
    public Task<Result<ShiftTradeDto>> DeclineTradeAsync(int id, CancellationToken cancellationToken = default) => StepAsync(id, "decline", cancellationToken);

    /// <inheritdoc />
    public Task<Result<ShiftTradeDto>> WithdrawTradeAsync(int id, CancellationToken cancellationToken = default) => StepAsync(id, "withdraw", cancellationToken);

    /// <inheritdoc />
    public Task<Result<ShiftTradeDto>> ApproveTradeAsync(int id, CancellationToken cancellationToken = default) => StepAsync(id, "approve", cancellationToken);

    /// <inheritdoc />
    public Task<Result<ShiftTradeDto>> RejectTradeAsync(int id, CancellationToken cancellationToken = default) => StepAsync(id, "reject", cancellationToken);

    // An offset such as +01:00 holds a plus, which a query would read as a space.
    private static string Moment(DateTimeOffset value) =>
        Uri.EscapeDataString(value.ToString("O", CultureInfo.InvariantCulture));

    private async Task<Result<ShiftTradeDto>> StepAsync(int id, string step, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        return await _api.PostAsync<ShiftTradeDto>($"{ApiRoutes.ShiftTrades}/{id}/{step}", null, cancellationToken).ConfigureAwait(false);
    }
}
