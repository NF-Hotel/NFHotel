using System.Text.Json;

using NFHotel.Api.Contracts;
using NFHotel.ApiClient;
using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

using NSubstitute;

namespace NFHotel.Api.Tests;

/// <summary>
/// The contract of ADR-0004: every method of the three use-case interfaces gives the same result, value or error
/// codes or exception, whether it is called on the service directly or through the client, the HTTP API and the
/// controllers. Both sides start from the same data and are driven in step, so state changes line up.
/// </summary>
public sealed class ContractParityTests : IAsyncLifetime
{
    private readonly ApiFactory _factory = new();
    private readonly ServiceWorld _direct = new();
    private Services? _directServices;
    private Services? _viaApi;

    #region Set-up

    public ValueTask InitializeAsync()
    {
        Seed(_direct);
        Seed(_factory.World);
        var authorization = Substitute.For<IStaffAuthorizationService>();

        authorization.HasPermissionAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        _directServices = new Services(
            _direct.BookingService(authorization),
            _direct.GuestService(authorization),
            _direct.RoomService());

        var gateway = new ApiGateway(_factory.CreateAuthorizedClient());
        _viaApi = new Services(new ApiBookingService(gateway), new ApiGuestService(gateway), new ApiRoomService(gateway));
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _factory.Dispose();
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Parity

    /// <summary>Every scenario gives the same description through the API as directly.</summary>
    [Fact]
    public async Task Scenarios_ThroughTheApiAndDirectly_GiveTheSameResults()
    {
        // Arrange
        var mismatches = new List<string>();

        // Act
        foreach (var (name, run) in Scenarios())
        {
            if (name.StartsWith("time:", StringComparison.Ordinal))
            {
                _direct.Clock.TimeOfDay = TimeSpan.FromHours(18);
                _factory.World.Clock.TimeOfDay = TimeSpan.FromHours(18);
                continue;
            }

            var direct = await run(_directServices!);
            var viaApi = await run(_viaApi!);
            if (direct != viaApi)
            {
                mismatches.Add($"{name}\n  direct: {direct}\n  api:    {viaApi}");
            }
        }

        // Assert
        Assert.True(mismatches.Count == 0, string.Join("\n", mismatches));
    }

    /// <summary>The scenarios touch every method of the three interfaces, so a new method cannot escape the contract.</summary>
    [Fact]
    public void Scenarios_AllInterfaceMethods_AreCovered()
    {
        // Arrange
        var names = Scenarios().Select(scenario => scenario.Name).ToArray();
        var methods = new[] { typeof(IBookingService), typeof(IGuestService), typeof(IRoomService) }
            .SelectMany(contract => contract.GetMethods().Select(method => $"{contract.Name}.{method.Name}"))
            .Distinct()
            .ToArray();

        // Act
        var uncovered = methods.Where(method => !names.Any(name => name.StartsWith(method, StringComparison.Ordinal))).ToArray();

        // Assert
        Assert.True(uncovered.Length == 0, $"Methods without a scenario: {string.Join(", ", uncovered)}");
    }

    #endregion

    #region Scenarios

    private static IReadOnlyList<(string Name, Func<Services, Task<string>> Run)> Scenarios()
    {
        var ct = TestContext.Current.CancellationToken;
        var today = new DateOnly(2026, 1, 1);
        var fields = new GuestFields("Maja", "Holm", "maja@example.com", "+4511223344", "Denmark", null);
        var list = new List<(string, Func<Services, Task<string>>)>();

        void Add(string name, Func<Services, Task<string>> run) => list.Add((name, run));
        void AddB<T>(string name, Func<IBookingService, Task<Result<T>>> call) => Add($"IBookingService.{name}", async s => Describe(await call(s.Bookings)));
        void AddG<T>(string name, Func<IGuestService, Task<Result<T>>> call) => Add($"IGuestService.{name}", async s => Describe(await call(s.Guests)));
        void AddR<T>(string name, Func<IRoomService, Task<Result<T>>> call) => Add($"IRoomService.{name}", async s => Describe(await call(s.Rooms)));
        void AddThrows(string name, Func<Services, Task> call) => Add(name, async s => await Throws(() => call(s)));

        // --- bookings: reading
        AddB("GetByIdAsync existing", b => b.GetByIdAsync(1, ct));
        AddB("GetByIdAsync unknown", b => b.GetByIdAsync(99, ct));
        AddThrows("IBookingService.GetByIdAsync key zero", s => s.Bookings.GetByIdAsync(0, ct));
        AddB("GetOverviewAsync month", b => b.GetOverviewAsync(new BookingOverviewQuery(today, today.AddDays(30)), ct));
        AddB("GetOverviewAsync search and sort", b => b.GetOverviewAsync(new BookingOverviewQuery(today, today.AddDays(60), "anna", BookingSortField.GuestName, SortDirection.Descending, IncludeCancelled: true), ct));
        AddB("GetOverviewAsync inverted period", b => b.GetOverviewAsync(new BookingOverviewQuery(today.AddDays(5), today), ct));
        AddB("GetOverviewAsync one day", b => b.GetOverviewAsync(new BookingOverviewQuery(today, today), ct));
        AddThrows("IBookingService.GetOverviewAsync null", s => s.Bookings.GetOverviewAsync(null!, ct));
        AddB("GetAvailableRoomsAsync free", b => b.GetAvailableRoomsAsync(today.AddDays(40), today.AddDays(42), null, ct));
        AddB("GetAvailableRoomsAsync busy and excluded", b => b.GetAvailableRoomsAsync(today, today.AddDays(2), 1, ct));
        AddB("GetAvailableRoomsAsync inverted", b => b.GetAvailableRoomsAsync(today.AddDays(3), today, null, ct));

        // --- bookings: creating
        AddB("CreateAsync existing guest", b => b.CreateAsync(new CreateBookingRequest(today.AddDays(20), today.AddDays(22), 3, 1, null), ct));
        AddB("CreateAsync overlap", b => b.CreateAsync(new CreateBookingRequest(today, today.AddDays(2), 1, 2, null), ct));
        AddB("CreateAsync new guest", b => b.CreateAsync(new CreateBookingRequest(today.AddDays(30), today.AddDays(31), 3, null, fields), ct));
        AddB("CreateAsync invalid new guest", b => b.CreateAsync(new CreateBookingRequest(today.AddDays(30), today.AddDays(31), 3, null, fields with { FirstName = " ", Email = "x" }), ct));
        AddB("CreateAsync neither guest path", b => b.CreateAsync(new CreateBookingRequest(today.AddDays(30), today.AddDays(31), 3, null, null), ct));
        AddB("CreateAsync inverted period", b => b.CreateAsync(new CreateBookingRequest(today.AddDays(9), today.AddDays(3), 3, 1, null), ct));
        AddB("CreateAsync unknown room", b => b.CreateAsync(new CreateBookingRequest(today.AddDays(30), today.AddDays(31), 99, 1, null), ct));
        AddThrows("IBookingService.CreateAsync null", s => s.Bookings.CreateAsync(null!, ct));

        // --- bookings: transitions and moving
        AddB("ConfirmAsync pending", b => b.ConfirmAsync(1, ct));
        AddB("ConfirmAsync twice", b => b.ConfirmAsync(1, ct));
        AddB("ConfirmAsync unknown", b => b.ConfirmAsync(99, ct));
        AddB("CheckInAsync today", b => b.CheckInAsync(1, ct));
        AddB("CheckInAsync later arrival", b => b.CheckInAsync(2, ct));
        AddB("RescheduleAsync checked in", b => b.RescheduleAsync(new RescheduleBookingRequest(1, today.AddDays(50), today.AddDays(52), 1), ct));
        AddB("RescheduleAsync free", b => b.RescheduleAsync(new RescheduleBookingRequest(2, today.AddDays(11), today.AddDays(13), 2), ct));
        AddThrows("IBookingService.RescheduleAsync key zero", s => s.Bookings.RescheduleAsync(new RescheduleBookingRequest(0, today, today.AddDays(1), 1), ct));
        Add("time:evening", _ => Task.FromResult(string.Empty));
        AddB("CheckOutAsync checked in", b => b.CheckOutAsync(1, ct));
        AddB("CheckOutAsync pending", b => b.CheckOutAsync(2, ct));
        AddB("CancelAsync pending", b => b.CancelAsync(2, ct));
        AddB("CancelAsync twice", b => b.CancelAsync(2, ct));
        AddB("GetByIdAsync after the changes", b => b.GetByIdAsync(1, ct));

        // --- guests
        AddG("GetByIdAsync existing", g => g.GetByIdAsync(1, ct));
        AddG("GetByIdAsync unknown", g => g.GetByIdAsync(99, ct));
        AddThrows("IGuestService.GetByIdAsync key zero", s => s.Guests.GetByIdAsync(0, ct));
        AddG("SearchAsync everyone", g => g.SearchAsync(null, ct));
        AddG("SearchAsync text", g => g.SearchAsync("anna jensen", ct));
        AddG("SearchAsync no match", g => g.SearchAsync("zzz", ct));
        AddG("CreateAsync valid", g => g.CreateAsync(new CreateGuestRequest(fields), ct));
        AddG("CreateAsync several errors", g => g.CreateAsync(new CreateGuestRequest(fields with { FirstName = " ", Email = "x", Country = " " }), ct));
        AddThrows("IGuestService.CreateAsync null", s => s.Guests.CreateAsync(null!, ct));
        AddG("UpdateAsync valid", g => g.UpdateAsync(new UpdateGuestRequest(1, fields with { LastName = "Nielsen" }), ct));
        AddG("UpdateAsync unknown", g => g.UpdateAsync(new UpdateGuestRequest(99, fields), ct));
        AddG("UpdateAsync invalid", g => g.UpdateAsync(new UpdateGuestRequest(1, fields with { Email = "x" }), ct));
        Add("IGuestService.DeleteAsync with bookings", async s => Describe(await s.Guests.DeleteAsync(2, ct)));
        Add("IGuestService.DeleteAsync without bookings", async s => Describe(await s.Guests.DeleteAsync(1, ct)));
        Add("IGuestService.DeleteAsync unknown", async s => Describe(await s.Guests.DeleteAsync(99, ct)));
        Add("IGuestService.ValidateFields valid", s => Task.FromResult(Describe(s.Guests.ValidateFields(fields))));
        Add("IGuestService.ValidateFields invalid", s => Task.FromResult(Describe(s.Guests.ValidateFields(fields with { FirstName = " " }))));

        // --- rooms: reading and master data
        AddR("GetByIdAsync existing", r => r.GetByIdAsync(1, ct));
        AddR("GetByIdAsync unknown", r => r.GetByIdAsync(99, ct));
        AddThrows("IRoomService.GetByIdAsync key zero", s => s.Rooms.GetByIdAsync(0, ct));
        AddR("GetAllAsync", r => r.GetAllAsync(ct));
        AddR("SearchAsync floor", r => r.SearchAsync(new RoomFilter(Floor: 1), ct));
        AddR("SearchAsync no match", r => r.SearchAsync(new RoomFilter(Floor: 77), ct));
        AddR("GetFilterOptionsAsync", r => r.GetFilterOptionsAsync(ct));
        AddR("CreateAsync valid", r => r.CreateAsync(new CreateRoomRequest("201", 2, RoomSize.Suite, 4), ct));
        AddR("CreateAsync duplicate number", r => r.CreateAsync(new CreateRoomRequest("201", 2, RoomSize.Suite, 4), ct));
        AddR("CreateAsync invalid", r => r.CreateAsync(new CreateRoomRequest(" ", 0, RoomSize.Single, 0), ct));
        AddR("UpdateAsync valid", r => r.UpdateAsync(new UpdateRoomRequest(3, "103", 1, RoomSize.Double, 3), ct));
        AddR("UpdateAsync number of another room", r => r.UpdateAsync(new UpdateRoomRequest(3, "101", 1, RoomSize.Double, 3), ct));
        AddR("UpdateAsync unknown", r => r.UpdateAsync(new UpdateRoomRequest(99, "9", 1, RoomSize.Double, 3), ct));
        Add("IRoomService.DeleteAsync with bookings", async s => Describe(await s.Rooms.DeleteAsync(1, ct)));
        Add("IRoomService.DeleteAsync without bookings", async s => Describe(await s.Rooms.DeleteAsync(2, ct)));
        Add("IRoomService.DeleteAsync unknown", async s => Describe(await s.Rooms.DeleteAsync(99, ct)));

        // --- rooms: transitions
        AddR("TakeOutOfServiceAsync", r => r.TakeOutOfServiceAsync(3, ct));
        AddR("SendToMaintenanceAsync", r => r.SendToMaintenanceAsync(3, ct));
        AddR("ReturnToServiceAsync", r => r.ReturnToServiceAsync(3, ct));
        AddR("MarkDailyCleaningDueAsync", r => r.MarkDailyCleaningDueAsync(3, ct));
        AddR("StartCleaningAsync", r => r.StartCleaningAsync(3, ct));
        AddR("MarkDepartureCleaningDueAsync refused while cleaning", r => r.MarkDepartureCleaningDueAsync(3, ct));
        AddR("CompleteCleaningAsync", r => r.CompleteCleaningAsync(3, ct));
        AddR("ReportServiceNeededAsync", r => r.ReportServiceNeededAsync(3, ct));
        AddR("StartServiceAsync", r => r.StartServiceAsync(3, ct));
        AddR("CompleteServiceAsync needing cleaning", r => r.CompleteServiceAsync(3, true, ct));
        AddR("StartServiceAsync refused on a room that needs cleaning", r => r.StartServiceAsync(3, ct));
        AddR("MarkDepartureCleaningDueAsync", r => r.MarkDepartureCleaningDueAsync(3, ct));
        AddR("TakeOutOfServiceAsync unknown", r => r.TakeOutOfServiceAsync(99, ct));

        return list;
    }

    #endregion

    #region Helpers

    private static void Seed(ServiceWorld world)
    {
        var rooms = new[] { world.AddRoom("101", 1), world.AddRoom("102", 1), world.AddRoom("103", 2) };
        var anna = world.AddGuest("Anna", "Jensen");
        var bo = world.AddGuest("Bo", "Larsen");
        world.AddBooking(rooms[0].Id, anna.Id, startOffsetDays: 0, nights: 3);
        world.AddBooking(rooms[1].Id, bo.Id, startOffsetDays: 10, nights: 2);
        world.Rooms.RoomIdsWithBookings.Add(rooms[0].Id);
        world.Guests.GuestIdsWithBookings.Add(bo.Id);
    }

    private static string Describe<TValue>(Result<TValue> result) =>
        result.IsSuccess
            ? "ok " + JsonSerializer.Serialize(result.Value, ApiJson.Options)
            : "err " + string.Join(",", result.Errors.Select(error => error.Code));

    private static string Describe(Result result) =>
        result.IsSuccess ? "ok" : "err " + string.Join(",", result.Errors.Select(error => error.Code));

    private static async Task<string> Throws(Func<Task> call)
    {
        try
        {
            await call();
            return "no exception";
        }
        catch (Exception exception)
        {
            return "throws " + exception.GetType().Name;
        }
    }

    private sealed record Services(IBookingService Bookings, IGuestService Guests, IRoomService Rooms);

    #endregion
}
