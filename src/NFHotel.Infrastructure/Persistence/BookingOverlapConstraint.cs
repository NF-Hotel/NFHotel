using System.Globalization;

using NFHotel.Domain.ValueObjects;

namespace NFHotel.Infrastructure.Persistence;

/// <summary>
/// Databasens udgave af overlapsreglen: den genererede kolonne <c>effective_end_date</c>
/// og exclusion constrainten <c>ex_booking_room_period</c> (A-01, A-02, B-03).
/// </summary>
/// <remarks>
/// <b>Definitionen findes ét sted (A-04).</b> Overlapsreglen er formuleret i
/// <see cref="NFHotel.Domain.Entities.Booking"/>, og alt SQL herunder udledes af den:
/// <list type="bullet">
/// <item><description>
/// <see cref="BlockingStatusPredicate"/> genereres ud fra
/// <see cref="NFHotel.Domain.ValueObjects.BookingStatusExtensions.BlocksRoom"/>. Ændrer domænet hvilke statusser der spærrer,
/// ændrer prædikatet sig med — det kan ikke drive fra hinanden ved et uheld.
/// </description></item>
/// <item><description>
/// <see cref="EffectiveEndDateSql"/> beregner præcis samme udtryk som
/// <c>Booking.EffectiveEndDate</c>: <c>GREATEST(StartDate + 1, COALESCE(CheckOutDate, EndDate))</c>.
/// </description></item>
/// <item><description>
/// <c>'[)'</c> er halvåbent og svarer til <c>DateRange.Overlaps</c>'
/// <c>Start &lt; other.End &amp;&amp; End &gt; other.Start</c> (BR-39).
/// </description></item>
/// </list>
/// <para>
/// <b>Kontrakt til Infrastructure.Tests:</b> paritetstesten skal (1) asserte at
/// <see cref="BlockingStatuses"/> er identisk med
/// <c>Enum.GetValues&lt;BookingStatus&gt;().Where(BookingStatusExtensions.BlocksRoom)</c>, og (2) køre
/// et sæt kendte periodepar mod både <see cref="NFHotel.Domain.Entities.Booking.ConflictsWith"/> og en rigtig
/// PostgreSQL med denne constraint, og kræve samme svar. Uden den test er der ingen der
/// opdager at C# og SQL er blevet uenige om <c>&lt;</c> mod <c>&lt;=</c>.
/// </para>
/// <para>
/// Bemærk at <see cref="CheckOutDateColumn"/> er en selvstændig kolonne og ikke udledt af
/// <c>check_out_time</c> (A-02): omregningen tidsstempel → hotel-lokal dato er
/// <c>STABLE</c>, ikke <c>IMMUTABLE</c>, og må derfor hverken indgå i en genereret kolonne
/// eller i en exclusion constraint.
/// </para>
/// </remarks>
public static class BookingOverlapConstraint
{
    /// <summary>Tabellen constrainten sidder på.</summary>
    public const string TableName = "booking";

    /// <summary>Navnet på exclusion constrainten.</summary>
    public const string ConstraintName = "ex_booking_room_period";

    /// <summary>PostgreSQL-extensionen constrainten kræver for at kunne blande <c>=</c> og <c>&amp;&amp;</c>.</summary>
    public const string RequiredExtension = "btree_gist";

    /// <summary>Kolonnen der bærer ankomstdatoen.</summary>
    public const string StartDateColumn = "start_date";

    /// <summary>Kolonnen der bærer den bookede afrejsedato (historik, A-01).</summary>
    public const string EndDateColumn = "end_date";

    /// <summary>Kolonnen der bærer den faktiske, hotel-lokale udtjekningsdato (A-02).</summary>
    public const string CheckOutDateColumn = "check_out_date";

    /// <summary>Den genererede kolonne der bærer den effektive slutdato (A-01).</summary>
    public const string EffectiveEndDateColumn = "effective_end_date";

    /// <summary>Skyggeegenskaben EF Core mapper <see cref="EffectiveEndDateColumn"/> til.</summary>
    /// <remarks>
    /// <c>Booking.EffectiveEndDate</c> er en beregnet C#-egenskab uden setter og uden
    /// backing field, og kan derfor ikke materialiseres af EF. Kolonnen mappes i stedet som
    /// skyggeegenskab og læses i projektioner via
    /// <c>EF.Property&lt;DateOnly&gt;(booking, EffectiveEndDateProperty)</c>.
    /// <para>
    /// Navnet må <b>ikke</b> være <c>EffectiveEndDate</c>: EF ville da binde skyggeegenskaben
    /// til den beregnede C#-egenskab og fejle med "No backing field could be found".
    /// </para>
    /// </remarks>
    public const string EffectiveEndDateProperty = "StoredEffectiveEndDate";

    /// <summary>Kolonnen der bærer bookingens status.</summary>
    public const string StatusColumn = "status";

    /// <summary>Kolonnen der bærer rummets fremmednøgle (A-15).</summary>
    public const string RoomIdColumn = "room_id";

    /// <summary>
    /// SQL-udtrykket bag den genererede kolonne. Samme udtryk som <c>Booking.EffectiveEndDate</c>.
    /// </summary>
    public const string EffectiveEndDateSql =
        $"GREATEST({StartDateColumn} + 1, COALESCE({CheckOutDateColumn}, {EndDateColumn}))";

    /// <summary>
    /// De statusser der spærrer rummet, udledt af <see cref="NFHotel.Domain.ValueObjects.BookingStatusExtensions.BlocksRoom"/> (A-04).
    /// </summary>
    public static IReadOnlyList<BookingStatus> BlockingStatuses { get; } =
        Enum.GetValues<BookingStatus>().Where(BookingStatusExtensions.BlocksRoom).ToArray();

    /// <summary>
    /// De statusser der IKKE spærrer rummet. I dag kun <see cref="BookingStatus.Cancelled"/>.
    /// </summary>
    public static IReadOnlyList<BookingStatus> NonBlockingStatuses { get; } =
        Enum.GetValues<BookingStatus>().Where(status => !status.BlocksRoom()).ToArray();

    /// <summary>
    /// Constraintens <c>WHERE</c>-prædikat, fx <c>status &lt;&gt; 4</c>. Genereret ud fra
    /// <see cref="NonBlockingStatuses"/>, aldrig skrevet i hånden.
    /// </summary>
    public static string BlockingStatusPredicate { get; } = BuildBlockingStatusPredicate();

    /// <summary>SQL der opretter den extension constrainten kræver.</summary>
    public static string CreateExtensionSql =>
        $"CREATE EXTENSION IF NOT EXISTS {RequiredExtension};";

    /// <summary>SQL der opretter exclusion constrainten (Architecture.md afsnit 5).</summary>
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

    /// <summary>SQL der fjerner exclusion constrainten igen. En migration uden virksom <c>Down</c> er ikke færdig.</summary>
    public static string DropConstraintSql =>
        $"ALTER TABLE {TableName} DROP CONSTRAINT IF EXISTS {ConstraintName};";

    /// <summary>
    /// Bygger <c>WHERE</c>-prædikatet ud fra de statusser der ikke spærrer.
    /// </summary>
    /// <returns>
    /// <c>status &lt;&gt; n</c> ved præcis én ikke-spærrende status, <c>status &lt;&gt; ALL (ARRAY[..])</c>
    /// ved flere, og <c>TRUE</c> hvis alle statusser spærrer.
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