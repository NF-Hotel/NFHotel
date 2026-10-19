using System.Globalization;

using NFHotel.Domain.ValueObjects;

namespace NFHotel.Infrastructure.Persistence;

/// <summary>
/// The database's version of the overlap rule: the generated column <c>effective_end_date</c>
/// and the exclusion constraint <c>ex_booking_room_period</c> (A-01, A-02, B-03).
/// </summary>
/// <remarks>
/// <b>The definition exists in one place (A-04).</b> The overlap rule is formulated in
/// <see cref="NFHotel.Domain.Entities.Booking"/>, and all SQL below is derived from it:
/// <list type="bullet">
/// <item><description>
/// <see cref="BlockingStatusPredicate"/> is generated from
/// <see cref="NFHotel.Domain.ValueObjects.BookingStatusExtensions.BlocksRoom"/>. If the domain changes which statuses block,
/// the predicate changes with it — it cannot drift apart by accident.
/// </description></item>
/// <item><description>
/// <see cref="EffectiveEndDateSql"/> computes exactly the same expression as
/// <c>Booking.EffectiveEndDate</c>: <c>GREATEST(StartDate + 1, COALESCE(CheckOutDate, EndDate))</c>.
/// </description></item>
/// <item><description>
/// <c>'[)'</c> is half-open and corresponds to <c>DateRange.Overlaps</c>'
/// <c>Start &lt; other.End &amp;&amp; End &gt; other.Start</c> (BR-39).
/// </description></item>
/// </list>
/// <para>
/// <b>Contract for Infrastructure.Tests:</b> the parity test must (1) assert that
/// <see cref="BlockingStatuses"/> is identical to
/// <c>Enum.GetValues&lt;BookingStatus&gt;().Where(BookingStatusExtensions.BlocksRoom)</c>, and (2) run
/// a set of known period pairs against both <see cref="NFHotel.Domain.Entities.Booking.ConflictsWith"/> and a real
/// PostgreSQL with this constraint, and require the same answer. Without that test nobody
/// will notice that C# and SQL have come to disagree about <c>&lt;</c> versus <c>&lt;=</c>.
/// </para>
/// <para>
/// Note that <see cref="CheckOutDateColumn"/> is a column of its own and not derived from
/// <c>check_out_time</c> (A-02): converting a timestamp → hotel-local date is
/// <c>STABLE</c>, not <c>IMMUTABLE</c>, and may therefore be part of neither a generated column
/// nor an exclusion constraint.
/// </para>
/// </remarks>
public static class BookingOverlapConstraint
{
    /// <summary>The table the constraint sits on.</summary>
    public const string TableName = "booking";

    /// <summary>The name of the exclusion constraint.</summary>
    public const string ConstraintName = "ex_booking_room_period";

    /// <summary>The PostgreSQL extension the constraint requires to be able to mix <c>=</c> and <c>&amp;&amp;</c>.</summary>
    public const string RequiredExtension = "btree_gist";

    /// <summary>The column that carries the arrival date.</summary>
    public const string StartDateColumn = "start_date";

    /// <summary>The column that carries the booked departure date (history, A-01).</summary>
    public const string EndDateColumn = "end_date";

    /// <summary>The column that carries the actual, hotel-local check-out date (A-02).</summary>
    public const string CheckOutDateColumn = "check_out_date";

    /// <summary>The generated column that carries the effective end date (A-01).</summary>
    public const string EffectiveEndDateColumn = "effective_end_date";

    /// <summary>The shadow property EF Core maps <see cref="EffectiveEndDateColumn"/> to.</summary>
    /// <remarks>
    /// <c>Booking.EffectiveEndDate</c> is a computed C# property without setter and without
    /// backing field, and therefore cannot be materialised by EF. The column is instead mapped as a
    /// shadow property and read in projections via
    /// <c>EF.Property&lt;DateOnly&gt;(booking, EffectiveEndDateProperty)</c>.
    /// <para>
    /// The name must <b>not</b> be <c>EffectiveEndDate</c>: EF would then bind the shadow property
    /// to the computed C# property and fail with "No backing field could be found".
    /// </para>
    /// </remarks>
    public const string EffectiveEndDateProperty = "StoredEffectiveEndDate";

    /// <summary>The column that carries the status of the booking.</summary>
    public const string StatusColumn = "status";

    /// <summary>The column that carries the foreign key of the room (A-15).</summary>
    public const string RoomIdColumn = "room_id";

    /// <summary>
    /// The SQL expression behind the generated column. The same expression as <c>Booking.EffectiveEndDate</c>.
    /// </summary>
    public const string EffectiveEndDateSql =
        $"GREATEST({StartDateColumn} + 1, COALESCE({CheckOutDateColumn}, {EndDateColumn}))";

    /// <summary>
    /// The statuses that block the room, derived from <see cref="NFHotel.Domain.ValueObjects.BookingStatusExtensions.BlocksRoom"/> (A-04).
    /// </summary>
    public static IReadOnlyList<BookingStatus> BlockingStatuses { get; } =
        Enum.GetValues<BookingStatus>().Where(BookingStatusExtensions.BlocksRoom).ToArray();

    /// <summary>
    /// The statuses that do NOT block the room. Today only <see cref="BookingStatus.Cancelled"/>.
    /// </summary>
    public static IReadOnlyList<BookingStatus> NonBlockingStatuses { get; } =
        Enum.GetValues<BookingStatus>().Where(status => !status.BlocksRoom()).ToArray();

    /// <summary>
    /// The <c>WHERE</c> predicate of the constraint, for example <c>status &lt;&gt; 4</c>. Generated from
    /// <see cref="NonBlockingStatuses"/>, never written by hand.
    /// </summary>
    public static string BlockingStatusPredicate { get; } = BuildBlockingStatusPredicate();

    /// <summary>SQL that creates the extension the constraint requires.</summary>
    public static string CreateExtensionSql =>
        $"CREATE EXTENSION IF NOT EXISTS {RequiredExtension};";

    /// <summary>SQL that creates the exclusion constraint (Architecture.md section 5).</summary>
    public static string CreateConstraintSql =>
        $"""
         ALTER TABLE {TableName}
           ADD CONSTRAINT {ConstraintName}
           EXCLUDE USING gist (
               {RoomIdColumn} WITH =,
               daterange({StartDateColumn}, {EffectiveEndDateColumn}, '[)') WITH &&
           )
           WHERE ({BlockingStatusPredicate});
         """;

    /// <summary>SQL that removes the exclusion constraint again. A migration without an effective <c>Down</c> is not finished.</summary>
    public static string DropConstraintSql =>
        $"ALTER TABLE {TableName} DROP CONSTRAINT IF EXISTS {ConstraintName};";

    /// <summary>
    /// Builds the <c>WHERE</c> predicate from the statuses that do not block.
    /// </summary>
    /// <returns>
    /// <c>status &lt;&gt; n</c> for exactly one non-blocking status, <c>status &lt;&gt; ALL (ARRAY[..])</c>
    /// for several, and <c>TRUE</c> if all statuses block.
    /// </returns>
    private static string BuildBlockingStatusPredicate()
    {
        var ordinals = NonBlockingStatuses
            .Select(status => ((int)status).ToString(CultureInfo.InvariantCulture))
            .ToArray();

        return ordinals.Length switch
        {
            0 => "TRUE",
            1 => $"{StatusColumn} <> {ordinals[0]}",
            _ => $"{StatusColumn} <> ALL (ARRAY[{string.Join(", ", ordinals)}])"
        };
    }
}
