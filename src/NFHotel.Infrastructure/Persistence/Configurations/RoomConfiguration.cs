using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NFHotel.Domain.Entities;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core-mapping for <see cref="Room"/>.
/// </summary>
/// <remarks>
/// Længdegrænsen spejler domænets konstant frem for at gentage tallet (A-10): ellers ville
/// en for lang værdi fejle som <c>DbUpdateException</c> i stedet for som en domænefejl.
/// </remarks>
public sealed class RoomConfiguration : EntityConfiguration<Room, int>
{
    /// <inheritdoc />
    protected override string TableName => "room";

    /// <inheritdoc />
    protected override void ConfigureTable(TableBuilder<Room> table)
    {
        // BR-98 og BR-100 som databaseværn bag domæneinvarianten (BR-N-04).
        table.HasCheckConstraint("ck_room_floor_positive", "floor > 0");
        table.HasCheckConstraint("ck_room_capacity_positive", "capacity > 0");
    }

    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<Room> builder)
    {
        // Det gamle skema sagde INT i det ene SQL-sæt og string i C#. Ét skema, én type.
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

        // BR-N-01: to rum med samme nummer er meningsløst. Det gamle skema havde ingen
        // constraint. Servicen tjekker inden den skriver; indekset er kapløbsværnet bag det.
        builder.HasIndex(room => room.RoomNumber)
            .IsUnique()
            .HasDatabaseName("ux_room_room_number");

        // Tilgængelighedsopslaget filtrerer på bookbarhed (BR-110).
        builder.HasIndex(room => room.Status)
            .HasDatabaseName("ix_room_status");

        builder.Ignore(room => room.IsBookable);

        builder.UseXminAsConcurrencyToken();
    }
}