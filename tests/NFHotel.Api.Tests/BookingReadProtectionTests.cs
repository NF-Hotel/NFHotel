using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using NFHotel.Api.Contracts;
using NFHotel.Application.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Api.Tests;

/// <summary>
/// A booking is read only by staff who may see the bookings, or by the guest who gives the number and the email of the
/// booking (MIL-079). The tests were written before the fix: on that code the read by id answered every holder of the
/// API token, and the lookup did not exist.
/// </summary>
/// <remarks>
/// The addresses and bodies are written out instead of taken from the contracts, so the tests pin what the web
/// application depends on, and so they compiled and failed against the code before the fix.
/// </remarks>
public sealed class BookingReadProtectionTests : IClassFixture<ApiFactory>
{
    private const string ReceiptPath = "api/bookings/public/receipt";
    private const string GuestEmail = "anna@example.com";
    private const int MaxFailures = 5;

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

    #region Lookup with number and email

    /// <summary>Positive: the right number and email give the receipt, and the answer holds nothing of the guest.</summary>
    [Fact]
    public async Task LookUpReceipt_RightNumberAndEmail_ReturnsOnlyTheReceipt()
    {
        // Arrange
        var bookingId = SeedBooking();
        using var client = _factory.CreateServiceClient();

        // Act
        using var response = await LookUpAsync(client, bookingId, GuestEmail);
        var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(text);
        var receipt = body.RootElement.GetProperty("value");
        Assert.Equal($"FLZ-{bookingId:D6}", receipt.GetProperty("bookingNumber").GetString());
        Assert.False(receipt.TryGetProperty("guest", out _));
        Assert.DoesNotContain(GuestEmail, text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Jensen", text, StringComparison.Ordinal);
        Assert.DoesNotContain("12345678", text, StringComparison.Ordinal);
    }

    /// <summary>Edge case: the email is compared without letter case and surrounding spaces.</summary>
    [Theory]
    [InlineData("ANNA@EXAMPLE.COM")]
    [InlineData("  Anna@Example.com  ")]
    public async Task LookUpReceipt_EmailInOtherCaseOrWithSpaces_ReturnsTheReceipt(string email)
    {
        // Arrange
        var bookingId = SeedBooking();
        using var client = _factory.CreateServiceClient();

        // Act
        using var response = await LookUpAsync(client, bookingId, email);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Negative: a wrong email and an unknown number give the same 404 with the same body, so nothing tells a guesser
    /// whether a number exists.
    /// </summary>
    [Fact]
    public async Task LookUpReceipt_WrongEmailAndUnknownNumber_GiveTheSameAnswer()
    {
        // Arrange
        var bookingId = SeedBooking();
        using var client = _factory.CreateServiceClient();

        // Act
        using var wrongEmail = await LookUpAsync(client, bookingId, "someone.else@example.com");
        using var unknownNumber = await LookUpAsync(client, 999_999, GuestEmail);
        var wrongEmailText = await wrongEmail.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var unknownNumberText = await unknownNumber.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, wrongEmail.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknownNumber.StatusCode);
        Assert.Contains(ErrorCodes.Booking.NotFound, wrongEmailText, StringComparison.Ordinal);
        Assert.Equal(wrongEmailText, unknownNumberText);
    }

    /// <summary>
    /// Boundary value: five failed lookups of one number are answered 404; the sixth within the window is refused with
    /// 429, even with the right email.
    /// </summary>
    [Fact]
    public async Task LookUpReceipt_SixthAttemptAfterFiveFailures_ReturnsTooManyRequests()
    {
        // Arrange
        var bookingId = SeedBooking();
        using var client = _factory.CreateServiceClient();
        var failures = new List<HttpStatusCode>();
        for (var attempt = 0; attempt < MaxFailures; attempt++)
        {
            using var failed = await LookUpAsync(client, bookingId, $"guess-{attempt}@example.com");
            failures.Add(failed.StatusCode);
        }

        // Act
        using var response = await LookUpAsync(client, bookingId, GuestEmail);

        // Assert
        Assert.All(failures, status => Assert.Equal(HttpStatusCode.NotFound, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    /// <summary>
    /// Negative: a request without an email is refused as invalid input (422, as every validation error of the API),
    /// before any booking is read.
    /// </summary>
    [Fact]
    public async Task LookUpReceipt_WithoutEmail_ReturnsUnprocessableEntity()
    {
        // Arrange
        var bookingId = SeedBooking();
        using var client = _factory.CreateServiceClient();

        // Act
        using var response = await LookUpAsync(client, bookingId, "   ");
        var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("booking.receipt_email_required", text, StringComparison.Ordinal);
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

    private static Task<HttpResponseMessage> LookUpAsync(HttpClient client, int bookingId, string email) =>
        client.PostAsJsonAsync(ReceiptPath, new { bookingId, email }, ApiJson.Options, TestContext.Current.CancellationToken);

    #endregion
}
