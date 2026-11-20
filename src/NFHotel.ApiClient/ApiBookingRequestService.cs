using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;

namespace NFHotel.ApiClient;

/// <summary>
/// <see cref="IBookingRequestService"/> over HTTP (MIL-088, ADR-0052): the mode of the public booking, the booking request
/// and its verification go to the API as the public booking does, and the API checks the limits, sends the mail and
/// issues the guest token.
/// </summary>
public sealed class ApiBookingRequestService : IBookingRequestService
{
    #region Fields

    private readonly ApiGateway _api;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the service.</summary>
    /// <param name="api">The gateway to the API.</param>
    public ApiBookingRequestService(ApiGateway api)
    {
        ArgumentNullException.ThrowIfNull(api);

        _api = api;
    }

    #endregion Constructors

    #region Public methods

    /// <inheritdoc />
    public async Task<Result<PublicBookingModeDto>> GetModeAsync(CancellationToken cancellationToken = default) =>
        await _api
            .GetAsync<PublicBookingModeDto>($"{ApiRoutes.Bookings}/{ApiRoutes.PublicBookingMode}", cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    /// <remarks>The API answers 202 with the time the link lapses; the gateway reads every 2xx with a value as a success.</remarks>
    public async Task<Result<BookingRequestReceiptDto>> AskAsync(
        CreatePublicBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await _api
            .PostAsync<BookingRequestReceiptDto>($"{ApiRoutes.Bookings}/{ApiRoutes.PublicBookingRequests}", request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>
    /// The token goes in the body of the POST and never in the address, so no access log keeps it (ADR-0052 decision 3,
    /// ASVS 14.2.1).
    /// </remarks>
    public async Task<Result<PublicBookingDto>> VerifyAsync(
        VerifyBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await _api
            .PostAsync<PublicBookingDto>($"{ApiRoutes.Bookings}/{ApiRoutes.PublicBookingVerify}", request, cancellationToken)
            .ConfigureAwait(false);
    }

    #endregion Public methods
}
