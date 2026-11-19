using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Guests;

namespace NFHotel.ApiClient;

/// <summary>
/// <see cref="IGuestBookingService"/> over HTTP (ADR-0049 decision 3): the guest routes name no booking, because the
/// guest token in the scope of the call names it, and the answers hold the receipt alone.
/// </summary>
public sealed class ApiGuestBookingService : IGuestBookingService
{
    #region Fields

    private readonly ApiGateway _api;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the service.</summary>
    /// <param name="api">The gateway to the API.</param>
    public ApiGuestBookingService(ApiGateway api)
    {
        ArgumentNullException.ThrowIfNull(api);

        _api = api;
    }

    #endregion Constructors

    #region Public methods

    /// <inheritdoc />
    public async Task<Result<BookingReceiptDto>> GetAsync(CancellationToken cancellationToken = default) =>
        await _api.GetAsync<BookingReceiptDto>(ApiRoutes.GuestBooking, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Result<BookingReceiptDto>> CancelAsync(CancellationToken cancellationToken = default) =>
        await _api
            .PostAsync<BookingReceiptDto>($"{ApiRoutes.GuestBooking}/{ApiRoutes.GuestBookingCancel}", null, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Result<BookingReceiptDto>> RescheduleAsync(
        GuestRescheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await _api
            .PostAsync<BookingReceiptDto>($"{ApiRoutes.GuestBooking}/{ApiRoutes.GuestBookingReschedule}", request, cancellationToken)
            .ConfigureAwait(false);
    }

    #endregion Public methods
}
