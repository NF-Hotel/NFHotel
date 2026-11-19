using System.Collections.Concurrent;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NFHotel.ApiClient;
using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Domain.ValueObjects;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>
/// Hosts the real web application with substitutes for the guest sign-in and the guest booking use cases (MIL-018, and
/// since MIL-085 ADR-0049): the guest of one booking signs in with its <see cref="Reference"/> and <see cref="Email"/>,
/// and every call to the guest booking use cases records the tokens of its scope, so a test sees as whom a page spoke to
/// the API.
/// </summary>
public class GuestWebFactory : WebFactory
{
    /// <summary>The reference of the booking that the substitute knows, in its text form.</summary>
    public const string Reference = "3f2504e0-4f89-41d3-9a0c-0305e82c3301";

    /// <summary>The email the booking was made with.</summary>
    public const string Email = "anna@example.com";

    /// <summary>The guest token the substitute hands out, so a test can look for it in a cookie or a page.</summary>
    public const string GuestToken = "secret-guest-token-that-must-not-leak-0123456789";

    /// <summary>The end of the guest session the substitute hands out.</summary>
    public static readonly DateTimeOffset ExpiresAt = new(2100, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>Creates the factory and gives the substitutes their answers.</summary>
    public GuestWebFactory()
    {
        GuestSessionDto session = new(GuestToken, ExpiresAt);
        Access.SignInAsync(Arg.Any<GuestSignInRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            GuestSignInRequest request = call.Arg<GuestSignInRequest>();
            return Task.FromResult(
                string.Equals(request.Reference?.Trim(), Reference, StringComparison.OrdinalIgnoreCase)
                && string.Equals(request.Email?.Trim(), Email, StringComparison.OrdinalIgnoreCase)
                    ? Result<GuestSessionDto>.Success(session)
                    : Result<GuestSessionDto>.Failure(ErrorCodes.Api.GuestUnauthorized));
        });
        Bookings.GetAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<BookingReceiptDto>.Success(Receipt(BookingStatus.Pending))));
        Bookings.CancelAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<BookingReceiptDto>.Success(Receipt(BookingStatus.Cancelled))));
        Bookings.RescheduleAsync(Arg.Any<GuestRescheduleRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            GuestRescheduleRequest request = call.Arg<GuestRescheduleRequest>();
            return Task.FromResult(Result<BookingReceiptDto>.Success(
                Receipt(BookingStatus.Pending) with { StartDate = request.CheckInDate, EndDate = request.CheckOutDate }));
        });
    }

    /// <summary>The guest sign-in that the pages call instead of the API.</summary>
    public IGuestAccessService Access { get; } = Substitute.For<IGuestAccessService>();

    /// <summary>The guest booking use cases that the pages call instead of the API.</summary>
    public IGuestBookingService Bookings { get; } = Substitute.For<IGuestBookingService>();

    /// <summary>The staff and the guest token of the scope of each guest booking call, in order.</summary>
    public ConcurrentQueue<(string? Staff, string? Guest)> Callers { get; } = new();

    /// <summary>Opens the guest sign-in, fills the form and posts it.</summary>
    /// <param name="browser">The client that holds the cookies.</param>
    /// <param name="reference">The booking reference, as a guest would type it.</param>
    /// <param name="email">The email.</param>
    /// <returns>The answer to the post.</returns>
    public static async Task<HttpResponseMessage> SignInAsGuestAsync(HttpClient browser, string reference, string email)
    {
        ArgumentNullException.ThrowIfNull(browser);

        string html = await browser.GetStringAsync("/booking", TestContext.Current.CancellationToken);
        Dictionary<string, string> fields = HiddenFields(html);
        fields[InputName(html, "booking-reference")] = reference;
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
            services.RemoveAll<IGuestBookingService>();
            services.AddScoped(provider =>
            {
                Callers.Enqueue((provider.GetRequiredService<StaffCallContext>().Token, provider.GetRequiredService<GuestCallContext>().Token));
                return Bookings;
            });
        });
    }

    private static BookingReceiptDto Receipt(BookingStatus status) =>
        new(Guid.Parse(Reference), new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 12), 2, RoomSize.Double, status);
}
