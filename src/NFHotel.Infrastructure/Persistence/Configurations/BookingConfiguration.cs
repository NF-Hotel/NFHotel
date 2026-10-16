using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NFHotel.Domain.Entities;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core-mapping for <see cref="NFHotel.Domain.Entities.Booking"/>.
/// </summary>
/// <remarks>
/// Her mødes B-03 (exclusion constraint), B-06 (snake_case), B-07 (enum som int),
/// B-08 (<c>date</c> og <c>timestamptz</c>) og A-15 (<c>booking_id</c>).
/// <para>
/// Selve exclusion constrainten og den genererede kolonnes SQL kan EF ikke modellere fuldt
/// ud; definitionen ligger i <see cref="BookingOverlapConstraint"/> og skrives af
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
        // Databaseværn bag domænets tilstandsmaskine. Det gamle skema havde ingen
        // constraint, så en vilkårlig int kunne skrives i status-kolonnen.
        table.HasCheckConstraint(
            "ck_booking_status_range",
            $"{BookingOverlapConstraint.StatusColumn} BETWEEN 0 AND 4");

        // BR-103: 0 nætter er ikke en booking. Spejler DateRange-invarianten.
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

        // Npgsql kræver at DateTimeOffset-værdier har offset 0. Det holder, fordi alle
        // tidsstempler kommer fra IClock.UtcNow — aldrig fra DateTime.Now (A-05).
        builder.Property(booking => booking.CheckInTime)
            .HasColumnType("timestamptz");

        builder.Property(booking => booking.CheckOutTime)
            .HasColumnType("timestamptz");

        // A-02: selvstændig kolonne, sat af BookingService.CheckOutAsync. Må ikke udledes
        // af check_out_time — den omregning er STABLE, ikke IMMUTABLE, og kan derfor
        // hverken indgå i en genereret kolonne eller i en exclusion constraint.
        builder.Property(booking => booking.CheckOutDate)
            .HasColumnName(BookingOverlapConstraint.CheckOutDateColumn)
            .HasColumnType("date");

        builder.Property(booking => booking.Status)
            .HasConversion<int>()
            .IsRequired();

        // Afledte værdier uden kolonne. BookingNumber havde heller ikke en kolonne før.
        builder.Ignore(booking => booking.Period);
        builder.Ignore(booking => booking.EffectivePeriod);
        builder.Ignore(booking => booking.EffectiveEndDate);
        builder.Ignore(booking => booking.NumberOfNights);
        builder.Ignore(booking => booking.BookingNumber);
        builder.Ignore(booking => booking.CanConfirm);
        builder.Ignore(booking => booking.CanCheckOut);
        builder.Ignore(booking => booking.CanCancel);
        builder.Ignore(booking => booking.CanReschedule);

        // A-01: den effektive slutdato er en GENERATED ALWAYS ... STORED-kolonne, så
        // databasen selv holder den i sync med de tre kolonner den er beregnet af.
        // Den mappes som skyggeegenskab, fordi Booking.EffectiveEndDate er en beregnet
        // C#-egenskab uden setter og derfor ikke kan materialiseres af EF.
        builder.Property<DateOnly>(BookingOverlapConstraint.EffectiveEndDateProperty)
            .HasColumnName(BookingOverlapConstraint.EffectiveEndDateColumn)
            .HasColumnType("date")
            .HasComputedColumnSql(BookingOverlapConstraint.EffectiveEndDateSql, stored: true)
            .ValueGeneratedOnAddOrUpdate();

        // Restrict er det der bærer BR-78 og BR-112 som databaseværn: et rum eller en gæst
        // med bookinger kan ikke slettes. Servicen tjekker inden den sletter; fremmednøglen
        // er kapløbsværnet bag det tjek.
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

        // PostgreSQL indekserer ikke FK-siden automatisk. Begge bruges af oversigten og af
        // sletteguarden (BR-78, BR-112).
        builder.HasIndex(booking => booking.RoomId)
            .HasDatabaseName("ix_booking_room_id");

        builder.HasIndex(booking => booking.GuestId)
            .HasDatabaseName("ix_booking_guest_id");

        // BR-23 + BR-24: oversigten filtrerer altid på status OG periode, aldrig på status
        // alene. Det sammensatte indeks matcher derfor det faktiske query-mønster.
        builder.HasIndex(booking => new { booking.Status, booking.StartDate })
            .HasDatabaseName("ix_booking_status_start_date");

        // Optimistisk samtidighed gratis: PostgreSQLs systemkolonne xmin ændres ved hver
        // opdatering. Lukker det læs-så-skriv-hul det gamle BookingRepo.Update havde.
        builder.UseXminAsConcurrencyToken();
    }
}