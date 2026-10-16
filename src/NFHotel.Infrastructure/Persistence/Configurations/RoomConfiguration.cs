using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NFHotel.Domain.Entities;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core-mapping for <see cref="Room"/>.
/// </summary>
/// <remarks>
/// The length limit mirrors the constant of the domain instead of repeating the number (A-10): otherwise
/// a value that is too long would fail as <c>DbUpdateException</c> instead of as a domain error.
/// </remarks>
public sealed class RoomConfiguration : EntityConfiguration<Room, int>
{
    /// <inheritdoc />
    protected override string TableName => "room";

    /// <inheritdoc />
    protected override void ConfigureTable(TableBuilder<Room> table)
    {
        // BR-98 and BR-100 as database safeguards behind the domain invariant (BR-N-04).
        table.HasCheckConstraint("ck_room_floor_positive", "floor > 0");
        table.HasCheckConstraint("ck_room_capacity_positive", "capacity > 0");
    }

    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<Room> builder)
    {
        // The old schema said INT in one SQL set and string in C#. One schema, one type.
        builder.Property(room => room.RoomNumber)
            .HasMaxLength(Room.MaxRoomNumberLength)
            .IsRequired();

        builder.Property(room => room.Floor)
            .IsRequired();

        builder.Property(room => room.Capacity)
            .IsRequired();

        builder.Property(room => room.Size)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(room => room.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(room => room.HousekeepingStatus)
            .HasConversion<int>()
            .IsRequired();

        // BR-N-01: two rooms with the same number is meaningless. The old schema had no
        // constraint. The service checks before it writes; the index is the race safeguard behind that.
        builder.HasIndex(room => room.RoomNumber)
            .IsUnique()
            .HasDatabaseName("ux_room_room_number");

        // The availability lookup filters on bookability (BR-110).
        builder.HasIndex(room => room.Status)
            .HasDatabaseName("ix_room_status");

        builder.Ignore(room => room.IsBookable);

        builder.UseXminAsConcurrencyToken();
    }
}