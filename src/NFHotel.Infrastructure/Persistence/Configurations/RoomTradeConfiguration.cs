using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NFHotel.Domain.Entities;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="RoomTrade"/> to the table <c>room_trade</c>; the rooms are two integer arrays (MIL-060).</summary>
public sealed class RoomTradeConfiguration : EntityConfiguration<RoomTrade, int>
{
    /// <inheritdoc />
    protected override string TableName => "room_trade";

    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<RoomTrade> builder)
    {
        // Two keys of the same table: the role in front of the name of the table (KeyColumnNamingTests).
        builder.Property(trade => trade.FromStaffId).HasColumnName("from_staff_member_id");
        builder.Property(trade => trade.ToStaffId).HasColumnName("to_staff_member_id");
        builder.Property(trade => trade.GiveRoomIds).IsRequired();
        builder.Property(trade => trade.TakeRoomIds).IsRequired();
        builder.Property(trade => trade.State).HasConversion<int>().IsRequired();
        builder.Property(trade => trade.OfferedAt).IsRequired();
        builder.Ignore(trade => trade.IsPending);

        // A deleted account takes its trades with it.
        builder.HasOne<StaffMember>().WithMany().HasForeignKey(trade => trade.FromStaffId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<StaffMember>().WithMany().HasForeignKey(trade => trade.ToStaffId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(trade => trade.FromStaffId).HasDatabaseName("ix_room_trade_from_staff_id");
        builder.HasIndex(trade => trade.ToStaffId).HasDatabaseName("ix_room_trade_to_staff_id");
        builder.HasIndex(trade => trade.State).HasDatabaseName("ix_room_trade_state");
    }
}
