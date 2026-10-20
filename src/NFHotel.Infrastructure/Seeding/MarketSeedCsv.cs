using System.Globalization;

namespace NFHotel.Infrastructure.Seeding;

/// <summary>Reads <c>nf_hotel_bookings.csv</c>: semicolon-delimited, a header line, ISO dates (MIL-026).</summary>
public static class MarketSeedCsv
{
    private const char Separator = ';';

    private static readonly string[] RequiredColumns =
    [
        "booking_id", "is_canceled", "booking_date", "arrival_date", "stays_in_weekend_nights", "stays_in_week_nights",
        "adults", "children", "babies", "meal", "country", "market_segment", "is_repeated_guest", "previous_cancellations",
        "assigned_room_type", "booking_changes", "deposit_type", "customer_type", "required_car_parking_spaces",
        "total_of_special_requests",
    ];

    /// <summary>Reads every row.</summary>
    /// <param name="reader">The text of the file.</param>
    /// <returns>The rows in the order of the file.</returns>
    /// <exception cref="FormatException">The header lacks a column, or a row has a value that cannot be read; the message names the line.</exception>
    public static IReadOnlyList<MarketSeedRow> Parse(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var header = reader.ReadLine() ?? throw new FormatException("The file is empty.");
        var columns = header.TrimStart('﻿').Split(Separator).Select(name => name.Trim()).ToList();
        var missing = RequiredColumns.Where(name => !columns.Contains(name)).ToArray();
        if (missing.Length > 0)
        {
            throw new FormatException($"The header lacks the columns: {string.Join(", ", missing)}.");
        }

        var index = RequiredColumns.ToDictionary(name => name, name => columns.IndexOf(name));
        var rows = new List<MarketSeedRow>();
        var line = 1;
        while (reader.ReadLine() is { } text)
        {
            line++;
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var cells = text.Split(Separator);
            if (cells.Length < columns.Count)
            {
                throw new FormatException($"Line {line} has {cells.Length} values, expected {columns.Count}.");
            }

            try
            {
                rows.Add(ToRow(cells, index));
            }
            catch (Exception exception) when (exception is FormatException or OverflowException)
            {
                throw new FormatException($"Line {line}: {exception.Message}", exception);
            }
        }

        return rows;
    }

    private static MarketSeedRow ToRow(string[] cells, Dictionary<string, int> index)
    {
        string Text(string name) => cells[index[name]].Trim();
        int Number(string name) => int.Parse(Text(name), NumberStyles.None, CultureInfo.InvariantCulture);
        DateOnly Date(string name) => DateOnly.ParseExact(Text(name), "yyyy-MM-dd", CultureInfo.InvariantCulture);

        return new MarketSeedRow(
            Number("booking_id"),
            Number("is_canceled") == 1,
            Date("booking_date"),
            Date("arrival_date"),
            Math.Max(1, Number("stays_in_weekend_nights") + Number("stays_in_week_nights")),
            Number("adults"),
            Number("children"),
            Number("babies"),
            Text("meal"),
            Text("country"),
            Text("market_segment"),
            Number("is_repeated_guest") == 1,
            Number("previous_cancellations"),
            Text("assigned_room_type"),
            Number("booking_changes"),
            Text("deposit_type"),
            Text("customer_type"),
            Number("required_car_parking_spaces"),
            Number("total_of_special_requests"));
    }
}
