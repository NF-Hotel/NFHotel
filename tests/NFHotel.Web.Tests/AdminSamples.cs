using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.UseCases;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;

namespace NFHotel.Web.Tests;

/// <summary>Sample rows of the back office, for the tests of its view models and pages (MIL-054).</summary>
internal static class AdminSamples
{
    public static readonly DateOnly Today = new(2026, 3, 10);

    public static readonly BookingActionsDto NoActions = new(false, false, false, false, false);

    public static readonly BookingActionsDto AllActions = new(true, true, true, true, true);

    public static RoomListItemDto Room(
        int id = 1,
        RoomStatus status = RoomStatus.Available,
        HousekeepingStatus housekeeping = HousekeepingStatus.Clean,
        int floor = 1,
        RoomSize size = RoomSize.Double) =>
        new(id, $"{floor}0{id}", floor, size, 2, status, housekeeping);

    public static RoomDetailsDto RoomDetails(
        int id = 1,
        RoomStatus status = RoomStatus.Available,
        HousekeepingStatus housekeeping = HousekeepingStatus.Clean,
        bool hasBookings = false) =>
        new(id, $"10{id}", 1, RoomSize.Double, 2, status, housekeeping, status == RoomStatus.Available, hasBookings);

    public static AvailableRoomDto FreeRoom(int id = 1) => new(id, $"10{id}", 1, RoomSize.Double, 2);

    public static GuestListItemDto Guest(int id = 1, string firstName = "Rita", bool hasPassport = false) =>
        new(id, firstName, "Hansen", $"{firstName} Hansen", $"{firstName.ToLowerInvariant()}@example.com", "+4512345678", "Denmark", hasPassport);

    public static GuestDetailsDto GuestDetails(int id = 1, string? passport = null) =>
        new(id, "Rita", "Hansen", "Rita Hansen", "rita@example.com", "+4512345678", "Denmark", passport);

    public static BookingListItemDto BookingItem(
        int id = 1,
        BookingStatus status = BookingStatus.Pending,
        BookingActionsDto? actions = null,
        int roomId = 1) =>
        new(
            id,
            $"B-{id:0000}",
            Today,
            Today.AddDays(2),
            Today.AddDays(2),
            2,
            status,
            null,
            null,
            roomId,
            $"10{roomId}",
            1,
            "Rita Hansen",
            "Denmark",
            "rita@example.com",
            actions ?? AllActions);

    public static BookingDetailsDto BookingDetails(int id = 1, BookingStatus status = BookingStatus.Pending) =>
        new(
            id,
            $"B-{id:0000}",
            Today,
            Today.AddDays(2),
            Today.AddDays(2),
            2,
            status,
            null,
            null,
            null,
            Room(),
            Guest(),
            AllActions);

    /// <summary>The text the screen shows for an error code.</summary>
    public static string Text(string code) => ErrorMessages.Describe(new Error(code));

    public static Task<Result<T>> Ok<T>(T value) => Task.FromResult(Result<T>.Success(value));

    public static Task<Result<T>> Fail<T>(string code) => Task.FromResult(Result<T>.Failure(code));

    public static Task<Result<IReadOnlyList<T>>> List<T>(params T[] items) =>
        Task.FromResult(Result<IReadOnlyList<T>>.Success(items));
}
