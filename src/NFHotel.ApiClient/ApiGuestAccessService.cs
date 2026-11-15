using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Guests;

namespace NFHotel.ApiClient;

/// <summary>
/// <see cref="IGuestAccessService"/> over HTTP (MIL-018, ADR-0047): the sign-in and the public booking go to the API,
/// which checks the email and issues the guest token.
/// </summary>
public sealed class ApiGuestAccessService : IGuestAccessService
{
    #region Fields

    private readonly ApiGateway _api;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the service.</summary>
    /// <param name="api">The gateway to the API.</param>
    public ApiGuestAccessService(ApiGateway api)
    {
        ArgumentNullException.ThrowIfNull(api);

        _api = api;
    }

    #endregion Constructors

    #region Public methods

    /// <inheritdoc />
    /// <remarks>The sign-in carries no token of the caller, as the staff sign-in does not.</remarks>
    public async Task<Result<GuestSessionDto>> SignInAsync(
        BookingReceiptRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await _api
            .PostAnonymousAsync<GuestSessionDto>($"{ApiRoutes.Auth}/{ApiRoutes.GuestLogin}", request, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<PublicBookingDto>> BookAsync(
        CreatePublicBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await _api
            .PostAsync<PublicBookingDto>($"{ApiRoutes.Bookings}/{ApiRoutes.PublicBooking}", request, cancellationToken)
            .ConfigureAwait(false);
    }

    #endregion Public methods
}
