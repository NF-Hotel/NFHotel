using System.Collections.Concurrent;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NFHotel.ApiClient;
using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Domain.ValueObjects;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>
/// Hosts the real web application with substitutes for the guest sign-in and the booking use cases (MIL-018): the guest
/// of booking 7 signs in with its number and <see cref="Email"/>, and every call to the booking use cases records the
/// tokens of its scope, so a test sees as whom a page spoke to the API.
/// </summary>
public class GuestWebFactory : WebFactory
{
    /// <summary>The key of the booking that the substitute knows.</summary>
    public const int BookingId = 7;

    /// <summary>The number of that booking.</summary>
    public const string BookingNumber = "FLZ-000007";

    /// <summary>The email the booking was made with.</summary>
    public const string Email = "anna@example.com";

    /// <summary>The guest token the substitute hands out, so a test can look for it in a cookie or a page.</summary>
    public const string GuestToken = "secret-guest-token-that-must-not-leak-0123456789";

    /// <summary>The room of the booking, which a guest keeps when the dates change.</summary>
    public const int RoomId = 3;

    /// <summary>The end of the guest session the substitute hands out.</summary>
    public static readonly DateTimeOffset ExpiresAt = new(2100, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>Creates the factory and gives the substitutes their answers.</summary>
    public GuestWebFactory()
    {
        GuestSessionDto session = new(GuestToken, ExpiresAt, BookingId);
        Access.SignInAsync(Arg.Any<BookingReceiptRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            BookingReceiptRequest request = call.Arg<BookingReceiptRequest>();
            return Task.FromResult(
                request.BookingId == BookingId && string.Equals(request.Email?.Trim(), Email, StringComparison.OrdinalIgnoreCase)
                    ? Result<GuestSessionDto>.Success(session)
                    : Result<GuestSessionDto>.Failure(ErrorCodes.Api.GuestUnauthorized));
        });
        Bookings.GetByIdAsync(BookingId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<BookingDetailsDto>.Success(Booked(BookingStatus.Pending))));
        Bookings.CancelAsync(BookingId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<BookingDetailsDto>.Success(Booked(BookingStatus.Cancelled))));
        Bookings.RescheduleAsync(Arg.Any<RescheduleBookingRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            RescheduleBookingRequest request = call.Arg<RescheduleBookingRequest>();
            return Task.FromResult(Result<BookingDetailsDto>.Success(
                Booked(BookingStatus.Pending) with { StartDate = request.NewStartDate, EndDate = request.NewEndDate }));
        });
    }

    /// <summary>The guest sign-in that the pages call instead of the API.</summary>
    public IGuestAccessService Access { get; } = Substitute.For<IGuestAccessService>();

    /// <summary>The booking use cases that the pages call instead of the API.</summary>
    public IBookingService Bookings { get; } = Substitute.For<IBookingService>();

    /// <summary>The staff and the guest token of the scope of each booking call, in order.</summary>
    public ConcurrentQueue<(string? Staff, string? Guest)> Callers { get; } = new();

    /// <summary>Opens the guest sign-in, fills the form and posts it.</summary>
    /// <param name="browser">The client that holds the cookies.</param>
    /// <param name="bookingNumber">The booking number.</param>
    /// <param name="email">The email.</param>
    /// <returns>The answer to the post.</returns>
    public static async Task<HttpResponseMessage> SignInAsGuestAsync(HttpClient browser, string bookingNumber, string email)
    {
        ArgumentNullException.ThrowIfNull(browser);

        string html = await browser.GetStringAsync("/booking", TestContext.Current.CancellationToken);
        Dictionary<string, string> fields = HiddenFields(html);
        fields[InputName(html, "booking-number")] = bookingNumber;
        fields[InputName(html, "booking-email")] = email;
        using FormUrlEncodedContent form = new(fields);
        return await browser.PostAsync("/booking", form, TestContext.Current.CancellationToken);
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IGuestAccessService>();
            services.AddScoped(_ => Access);
            services.RemoveAll<IBookingService>();
            services.AddScoped(provider =>
            {
                Callers.Enqueue((provider.GetRequiredService<StaffCallContext>().Token, provider.GetRequiredService<GuestCallContext>().Token));
                return Bookings;
            });
        });
    }

    private static BookingDetailsDto Booked(BookingStatus status) =>
        new(
            BookingId,
            BookingNumber,
            new DateOnly(2026, 3, 10),
            new DateOnly(2026, 3, 12),
            new DateOnly(2026, 3, 12),
            2,
            status,
            null,
            null,
            null,
            new RoomListItemDto(RoomId, "103", 1, RoomSize.Double, 2, RoomStatus.Available, HousekeepingStatus.Clean),
            new GuestListItemDto(1, "Anna", "Jensen", "Anna Jensen", Email, "+4512345678", "Denmark", true),
            status == BookingStatus.Cancelled
                ? new BookingActionsDto(false, false, false, false, false)
                : new BookingActionsDto(true, false, false, true, true));
}
