using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NFHotel.Domain.Entities;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="ShiftTrade"/> to the table <c>shift_trade</c> (MIL-060).</summary>
public sealed class ShiftTradeConfiguration : EntityConfiguration<ShiftTrade, int>
{
    /// <inheritdoc />
    protected override string TableName => "shift_trade";

    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<ShiftTrade> builder)
    {
        // Two keys of the same table each: the role in front of the name of the table (KeyColumnNamingTests).
        builder.Property(trade => trade.OfferedById).HasColumnName("offered_by_staff_member_id");
        builder.Property(trade => trade.TakenById).HasColumnName("taken_by_staff_member_id");
        builder.Property(trade => trade.State).HasConversion<int>().IsRequired();
        builder.Property(trade => trade.OfferedAt).IsRequired();
        builder.Ignore(trade => trade.IsSwap);
        builder.Ignore(trade => trade.IsPending);

        // A deleted shift or account takes its trades with it.
        builder.HasOne<Shift>().WithMany().HasForeignKey(trade => trade.ShiftId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Shift>().WithMany().HasForeignKey(trade => trade.SwapShiftId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<StaffMember>().WithMany().HasForeignKey(trade => trade.OfferedById).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<StaffMember>().WithMany().HasForeignKey(trade => trade.TakenById).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(trade => trade.ShiftId).HasDatabaseName("ix_shift_trade_shift_id");
        builder.HasIndex(trade => trade.SwapShiftId).HasDatabaseName("ix_shift_trade_swap_shift_id");
        builder.HasIndex(trade => new { trade.State, trade.OfferedAt }).HasDatabaseName("ix_shift_trade_state_offered_at");
    }
}
