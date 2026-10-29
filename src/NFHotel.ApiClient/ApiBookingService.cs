using System.Globalization;

using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;

namespace NFHotel.ApiClient;

/// <summary>
/// <see cref="IBookingService"/> over HTTP. A key below one and a missing request throw the same exceptions
/// as the real service, before any call is made; everything else comes back as the same <see cref="Result"/>.
/// </summary>
public sealed class ApiBookingService : IBookingService
{
    private readonly ApiGateway _api;

    /// <summary>Creates the service.</summary>
    /// <param name="api">The gateway to the API.</param>
    public ApiBookingService(ApiGateway api)
    {
        ArgumentNullException.ThrowIfNull(api);
        _api = api;
    }

    /// <inheritdoc />
    public async Task<Result<BookingDetailsDto>> GetByIdAsync(int bookingId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bookingId);
        return await _api.GetAsync<BookingDetailsDto>($"{ApiRoutes.Bookings}/{bookingId}", cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<BookingListItemDto>>> GetOverviewAsync(BookingOverviewQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await _api.PostAsync<IReadOnlyList<BookingListItemDto>>($"{ApiRoutes.Bookings}/{ApiRoutes.Overview}", query, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AvailableRoomDto>>> GetAvailableRoomsAsync(
        DateOnly checkInDate,
        DateOnly checkOutDate,
        int? excludeBookingId,
        CancellationToken cancellationToken = default)
    {
        var path = string.Create(
            CultureInfo.InvariantCulture,
            $"{ApiRoutes.Bookings}/{ApiRoutes.AvailableRooms}?checkInDate={checkInDate:yyyy-MM-dd}&checkOutDate={checkOutDate:yyyy-MM-dd}");
        if (excludeBookingId is not null)
        {
            path += string.Create(CultureInfo.InvariantCulture, $"&excludeBookingId={excludeBookingId}");
        }

        return await _api.GetAsync<IReadOnlyList<AvailableRoomDto>>(path, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<BookingDetailsDto>> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await _api.PostAsync<BookingDetailsDto>(ApiRoutes.Bookings, request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<BookingDetailsDto>> CreatePublicAsync(
        CreateBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await _api.PostAsync<BookingDetailsDto>(
            $"{ApiRoutes.Bookings}/{ApiRoutes.PublicBooking}",
            request,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Result<BookingDetailsDto>> ConfirmAsync(int bookingId, CancellationToken cancellationToken = default) =>
        ActionAsync(bookingId, "confirm", cancellationToken);

    /// <inheritdoc />
    public Task<Result<BookingDetailsDto>> CheckInAsync(int bookingId, CancellationToken cancellationToken = default) =>
        ActionAsync(bookingId, "check-in", cancellationToken);

    /// <inheritdoc />
    public Task<Result<BookingDetailsDto>> CheckOutAsync(int bookingId, CancellationToken cancellationToken = default) =>
        ActionAsync(bookingId, "check-out", cancellationToken);

    /// <inheritdoc />
    public Task<Result<BookingDetailsDto>> CancelAsync(int bookingId, CancellationToken cancellationToken = default) =>
        ActionAsync(bookingId, "cancel", cancellationToken);

    /// <inheritdoc />
    public async Task<Result<BookingDetailsDto>> RescheduleAsync(RescheduleBookingRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.BookingId);
        return await _api.PostAsync<BookingDetailsDto>($"{ApiRoutes.Bookings}/{ApiRoutes.Reschedule}", request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<BookingDetailsDto>> ActionAsync(int bookingId, string action, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bookingId);
        return await _api.PostAsync<BookingDetailsDto>($"{ApiRoutes.Bookings}/{bookingId}/{action}", null, cancellationToken).ConfigureAwait(false);
    }
}
