using System.Globalization;
using System.Text;

using Microsoft.EntityFrameworkCore;

using NFHotel.Infrastructure.Persistence;

using Npgsql;

namespace NFHotel.Infrastructure.Seeding;

/// <summary>
/// Loads the fake bookings of <c>nf_hotel_bookings.csv</c> into an empty database: rooms, guests, bookings and the market
/// details of each booking (MIL-026). It writes with PostgreSQL <c>COPY</c> in one transaction, because the bookings are in
/// the past and <c>Booking.Create</c> refuses a start date in the past, and because there are tens of thousands of them.
/// It refuses a database that already has rooms, guests or bookings unless told to replace them.
/// </summary>
public sealed class MarketDataSeeder
{
    private const int CheckInHourUtc = 14;
    private const int CheckOutHourUtc = 10;
    private const int RoomAvailable = 0;
    private const int HousekeepingClean = 0;
    private const int StatusConfirmed = 1;
    private const int StatusCheckedIn = 2;
    private const int StatusCheckedOut = 3;
    private const int StatusCancelled = 4;

    private readonly HotelDbContext _context;

    /// <summary>Creates the seeder.</summary>
    /// <param name="context">The database context.</param>
    public MarketDataSeeder(HotelDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <summary>Writes the plan of a file to the database.</summary>
    /// <param name="plan">The rooms, guests and bookings.</param>
    /// <param name="today">
    /// Today's date in the hotel: a booking that left before it is checked out, one that is at the hotel is checked in, and one
    /// that arrives later is confirmed. A cancelled booking stays cancelled.
    /// </param>
    /// <param name="replace">True to delete the rooms, guests and bookings that exist; false to refuse when any exists.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>A task that ends when everything is written, or nothing is.</returns>
    /// <exception cref="InvalidOperationException">The database has data and <paramref name="replace"/> is false.</exception>
    public async Task SeedAsync(MarketSeedPlan plan, DateOnly today, bool replace, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var connection = (NpgsqlConnection)_context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        if (await HasDataAsync(connection, transaction, cancellationToken).ConfigureAwait(false))
        {
            if (!replace)
            {
                throw new InvalidOperationException("The database already has rooms, guests or bookings. Use --replace to delete them first.");
            }

            await ExecuteAsync(connection, transaction, "TRUNCATE TABLE booking, room, guest RESTART IDENTITY CASCADE", cancellationToken).ConfigureAwait(false);
        }

        await CopyRoomsAsync(connection, plan, cancellationToken).ConfigureAwait(false);
        await CopyGuestsAsync(connection, plan, cancellationToken).ConfigureAwait(false);
        await CopyBookingsAsync(connection, plan, today, cancellationToken).ConfigureAwait(false);
        await CopyDetailsAsync(connection, plan, cancellationToken).ConfigureAwait(false);

        foreach (var (table, column) in new[] { ("room", "room_id"), ("guest", "guest_id"), ("booking", "booking_id"), ("booking_market_detail", "booking_market_detail_id") })
        {
            await ExecuteAsync(
                connection,
                transaction,
                $"SELECT setval(pg_get_serial_sequence('{table}', '{column}'), COALESCE((SELECT MAX({column}) FROM {table}), 1), (SELECT COUNT(*) > 0 FROM {table}))",
                cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<bool> HasDataAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM booking) OR EXISTS (SELECT 1 FROM room) OR EXISTS (SELECT 1 FROM guest)",
            connection,
            transaction);

        return (bool)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string sql, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task CopyRoomsAsync(NpgsqlConnection connection, MarketSeedPlan plan, CancellationToken cancellationToken)
    {
        await using var writer = await connection
            .BeginTextImportAsync("COPY room (room_id, room_number, floor, size, capacity, status, housekeeping_status) FROM STDIN", cancellationToken)
            .ConfigureAwait(false);

        for (var number = 1; number <= plan.RoomCount; number++)
        {
            var size = MarketSeedPlanner.SizeOf(number);
            await writer.WriteLineAsync(Row(
                number,
                MarketSeedPlanner.RoomNumberText(number),
                MarketSeedPlanner.FloorOf(number),
                (int)size,
                MarketSeedPlanner.CapacityOf(size),
                RoomAvailable,
                HousekeepingClean)).ConfigureAwait(false);
        }
    }

    private static async Task CopyGuestsAsync(NpgsqlConnection connection, MarketSeedPlan plan, CancellationToken cancellationToken)
    {
        await using var writer = await connection
            .BeginTextImportAsync("COPY guest (guest_id, first_name, last_name, email, phone_number, country, passport_number) FROM STDIN", cancellationToken)
            .ConfigureAwait(false);

        for (var number = 1; number <= plan.GuestCountries.Count; number++)
        {
            await writer.WriteLineAsync(Row(
                number,
                "Guest",
                string.Create(CultureInfo.InvariantCulture, $"No. {number}"),
                string.Create(CultureInfo.InvariantCulture, $"guest{number}@example.test"),
                "+000 000 000",
                plan.GuestCountries[number - 1],
                null)).ConfigureAwait(false);
        }
    }

    private static async Task CopyBookingsAsync(NpgsqlConnection connection, MarketSeedPlan plan, DateOnly today, CancellationToken cancellationToken)
    {
        await using var writer = await connection
            .BeginTextImportAsync(
                "COPY booking (booking_id, start_date, end_date, check_in_time, check_out_time, check_out_date, status, room_id, guest_id) FROM STDIN",
                cancellationToken)
            .ConfigureAwait(false);

        foreach (var (row, room, guest) in plan.Bookings.Select(placement => (placement.Row, placement.Room, placement.Guest)))
        {
            var status = row.IsCanceled ? StatusCancelled
                : row.Departure <= today ? StatusCheckedOut
                : row.Arrival <= today ? StatusCheckedIn
                : StatusConfirmed;
            var checkedIn = status is StatusCheckedIn or StatusCheckedOut;
            var checkedOut = status == StatusCheckedOut;
            await writer.WriteLineAsync(Row(
                row.Number,
                Date(row.Arrival),
                Date(row.Departure),
                checkedIn ? Time(row.Arrival, CheckInHourUtc) : null,
                checkedOut ? Time(row.Departure, CheckOutHourUtc) : null,
                checkedOut ? Date(row.Departure) : null,
                status,
                room,
                guest)).ConfigureAwait(false);
        }
    }

    private static async Task CopyDetailsAsync(NpgsqlConnection connection, MarketSeedPlan plan, CancellationToken cancellationToken)
    {
        await using var writer = await connection
            .BeginTextImportAsync(
                "COPY booking_market_detail (booking_market_detail_id, booking_id, booked_at, price_per_night, adults, children, babies, meal, market_segment, "
                + "customer_type, deposit_type, room_type, is_repeated_guest, previous_cancellations, booking_changes, special_requests, parking_spaces) FROM STDIN",
                cancellationToken)
            .ConfigureAwait(false);

        foreach (var row in plan.Bookings.Select(placement => placement.Row))
        {
            await writer.WriteLineAsync(Row(
                row.Number,
                row.Number,
                Date(row.BookedAt),
                MarketSeedPricing.PricePerNight(row).ToString("0.00", CultureInfo.InvariantCulture),
                row.Adults,
                row.Children,
                row.Babies,
                row.Meal,
                row.MarketSegment,
                row.CustomerType,
                row.DepositType,
                row.RoomType,
                row.IsRepeatedGuest ? "t" : "f",
                row.PreviousCancellations,
                row.BookingChanges,
                row.SpecialRequests,
                row.ParkingSpaces)).ConfigureAwait(false);
        }
    }

    private static string Date(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Time(DateOnly date, int hourUtc) =>
        string.Create(CultureInfo.InvariantCulture, $"{date:yyyy-MM-dd} {hourUtc:00}:00:00+00");

    /// <summary>One line of PostgreSQL text COPY: tab-separated, <c>\N</c> for null, backslash escapes.</summary>
    private static string Row(params object?[] cells)
    {
        var line = new StringBuilder();
        for (var i = 0; i < cells.Length; i++)
        {
            if (i > 0)
            {
                line.Append('\t');
            }

            line.Append(cells[i] is null ? "\\N" : Escape(Convert.ToString(cells[i], CultureInfo.InvariantCulture) ?? string.Empty));
        }

        return line.ToString();
    }

    private static string Escape(string text) =>
        text.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal);
}
