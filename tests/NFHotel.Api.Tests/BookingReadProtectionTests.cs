using System.Net;
using System.Net.Http.Json;

using NFHotel.Api.Contracts;
using NFHotel.Application.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Api.Tests;

/// <summary>
/// A booking is read by id only by staff who may see the bookings (MIL-079), and the older lookup by number and email is
/// gone since MIL-085: a guest signs in with the reference and the email instead (ADR-0049). The tests of MIL-079 were
/// written before its fix: on that code the read by id answered every holder of the API token.
/// </summary>
/// <remarks>
/// The addresses and bodies are written out instead of taken from the contracts, so the tests pin what the web
/// application depends on, and so they compiled and failed against the code before the fix.
/// </remarks>
public sealed class BookingReadProtectionTests : IClassFixture<ApiFactory>
{
    private const string OldReceiptPath = "api/bookings/public/receipt";
    private const string GuestEmail = "anna@example.com";

    private readonly ApiFactory _factory;

    public BookingReadProtectionTests(ApiFactory factory)
    {
        _factory = factory;
    }

    #region Read by id

    /// <summary>Negative: the token of the web application alone does not read a booking; the answer is 401.</summary>
    [Fact]
    public async Task GetBooking_ApiTokenWithoutStaffToken_ReturnsUnauthorized()
    {
        // Arrange
        var bookingId = SeedBooking();
        using var client = _factory.CreateServiceClient();

        // Act
        using var response = await client.GetAsync($"api/bookings/{bookingId}", TestContext.Current.CancellationToken);
        var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.DoesNotContain(GuestEmail, text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Negative: staff without the permission to see the bookings get 403 and no booking.</summary>
    [Fact]
    public async Task GetBooking_HousekeepingWithoutBookingsView_ReturnsForbidden()
    {
        // Arrange
        var bookingId = SeedBooking();
        using var client = _factory.CreateStaffClient(StaffRole.Housekeeping);

        // Act
        using var response = await client.GetAsync($"api/bookings/{bookingId}", TestContext.Current.CancellationToken);
        var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains(ErrorCodes.Api.Forbidden, text, StringComparison.Ordinal);
        Assert.DoesNotContain(GuestEmail, text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Positive: a receptionist, who may see the bookings, reads the booking.</summary>
    [Fact]
    public async Task GetBooking_ReceptionistWithBookingsView_ReturnsOk()
    {
        // Arrange
        var bookingId = SeedBooking();
        using var client = _factory.CreateStaffClient(StaffRole.Receptionist);

        // Act
        using var response = await client.GetAsync($"api/bookings/{bookingId}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region The older lookup (MIL-085)

    /// <summary>
    /// ADR-0049 decision 8: the older lookup by number and email is gone, so the guest sign-in is the one door to keep
    /// right. Its route answers as a route that does not exist, with the email in the body or without.
    /// </summary>
    [Fact]
    public async Task OldLookup_AnyCall_IsNoEndpointAnyMore()
    {
        // Arrange
        int bookingId = SeedBooking();
        using HttpClient client = _factory.CreateServiceClient();

        // Act
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            OldReceiptPath, new { bookingId, email = GuestEmail }, ApiJson.Options, TestContext.Current.CancellationToken);
        string text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain(GuestEmail, text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Positive: the public part of the booking flow still works with the token of the web application alone.</summary>
    [Fact]
    public async Task AvailableRooms_ApiTokenWithoutStaffToken_StillReturnsOk()
    {
        // Arrange
        using var client = _factory.CreateServiceClient();
        var today = _factory.World.Clock.Today;

        // Act
        using var response = await client.GetAsync(
            $"api/bookings/available-rooms?checkInDate={today.AddDays(30):yyyy-MM-dd}&checkOutDate={today.AddDays(32):yyyy-MM-dd}",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Helpers

    private int SeedBooking()
    {
        var world = _factory.World;
        var room = world.AddRoom($"{world.Rooms.Items.Count + 1}1{world.Rooms.Items.Count}");
        var guest = world.AddGuest();
        var booking = world.AddBooking(room.Id, guest.Id, startOffsetDays: 5, nights: 2);
        return booking.Id;
    }


    #endregion
}
