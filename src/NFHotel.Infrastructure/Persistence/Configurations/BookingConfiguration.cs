using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NFHotel.Domain.Entities;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core-mapping for <see cref="NFHotel.Domain.Entities.Booking"/>.
/// </summary>
/// <remarks>
/// Here B-03 (exclusion constraint), B-06 (snake_case), B-07 (enum as int),
/// B-08 (<c>date</c> and <c>timestamptz</c>) and A-15 (<c>booking_id</c>) meet.
/// <para>
/// EF cannot fully model the exclusion constraint itself and the SQL of the generated column;
/// the definition is in <see cref="BookingOverlapConstraint"/> and is written by
/// migrationen.
/// </para>
/// </remarks>
public sealed class BookingConfiguration : EntityConfiguration<Booking, int>
{
    /// <inheritdoc />
    protected override string TableName => BookingOverlapConstraint.TableName;

    /// <inheritdoc />
    protected override void ConfigureTable(TableBuilder<Booking> table)
    {
        // Database safeguard behind the state machine of the domain. The old schema had no
        // constraint, so an arbitrary int could be written to the status column.
        table.HasCheckConstraint(
            "ck_booking_status_range",
            $"{BookingOverlapConstraint.StatusColumn} BETWEEN 0 AND 4");

        // BR-103: 0 nights is not a booking. Mirrors the DateRange invariant.
        table.HasCheckConstraint(
            "ck_booking_end_after_start",
            $"{BookingOverlapConstraint.EndDateColumn} > {BookingOverlapConstraint.StartDateColumn}");
    }

    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<Booking> builder)
    {
        builder.Property(booking => booking.StartDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(booking => booking.EndDate)
            .HasColumnType("date")
            .IsRequired();

        // Npgsql requires DateTimeOffset values to have offset 0. That holds, because all
        // timestamps come from IClock.UtcNow — never from DateTime.Now (A-05).
        builder.Property(booking => booking.CheckInTime)
            .HasColumnType("timestamptz");

        builder.Property(booking => booking.CheckOutTime)
            .HasColumnType("timestamptz");

        // A-02: a column of its own, set by BookingService.CheckOutAsync. Must not be derived
        // from check_out_time — that conversion is STABLE, not IMMUTABLE, and may therefore
        // be part of neither a generated column nor an exclusion constraint.
        builder.Property(booking => booking.CheckOutDate)
            .HasColumnName(BookingOverlapConstraint.CheckOutDateColumn)
            .HasColumnType("date");

        builder.Property(booking => booking.Status)
            .HasConversion<int>()
            .IsRequired();

        // Derived values without a column. BookingNumber did not have a column before either.
        builder.Ignore(booking => booking.Period);
        builder.Ignore(booking => booking.EffectivePeriod);
        builder.Ignore(booking => booking.EffectiveEndDate);
        builder.Ignore(booking => booking.NumberOfNights);
        builder.Ignore(booking => booking.BookingNumber);
        builder.Ignore(booking => booking.CanConfirm);
        builder.Ignore(booking => booking.CanCheckOut);
        builder.Ignore(booking => booking.CanCancel);
        builder.Ignore(booking => booking.CanReschedule);

        // A-01: the effective end date is a GENERATED ALWAYS ... STORED column, so
        // the database itself keeps it in sync with the three columns it is computed from.
        // It is mapped as a shadow property, because Booking.EffectiveEndDate is a computed
        // C# property without setter and therefore cannot be materialised by EF.
        builder.Property<DateOnly>(BookingOverlapConstraint.EffectiveEndDateProperty)
            .HasColumnName(BookingOverlapConstraint.EffectiveEndDateColumn)
            .HasColumnType("date")
            .HasComputedColumnSql(BookingOverlapConstraint.EffectiveEndDateSql, stored: true)
            .ValueGeneratedOnAddOrUpdate();

        // Restrict is what carries BR-78 and BR-112 as a database safeguard: a room or a guest
        // with bookings cannot be deleted. The service checks before it deletes; the foreign key
        // is the race safeguard behind that check.
        builder.HasOne(booking => booking.Room)
            .WithMany()
            .HasForeignKey(booking => booking.RoomId)
            .HasConstraintName("fk_booking_room_room_id")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(booking => booking.Guest)
            .WithMany()
            .HasForeignKey(booking => booking.GuestId)
            .HasConstraintName("fk_booking_guest_guest_id")
            .OnDelete(DeleteBehavior.Restrict);

        // PostgreSQL does not index the FK side automatically. Both are used by the overview and by
        // the delete guard (BR-78, BR-112).
        builder.HasIndex(booking => booking.RoomId)
            .HasDatabaseName("ix_booking_room_id");

        builder.HasIndex(booking => booking.GuestId)
            .HasDatabaseName("ix_booking_guest_id");

        // BR-23 + BR-24: the overview always filters on status AND period, never on status
        // alone. The composite index therefore matches the actual query pattern.
        builder.HasIndex(booking => new { booking.Status, booking.StartDate })
            .HasDatabaseName("ix_booking_status_start_date");

        // Optimistic concurrency for free: PostgreSQL's system column xmin changes on every
        // update. Closes the read-then-write gap that the old BookingRepo.Update had.
        builder.UseXminAsConcurrencyToken();
    }
}