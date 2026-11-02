using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NFHotel.Domain.Entities;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="RoomRate"/> to the table <c>room_rate</c> (MIL-055).</summary>
public sealed class RoomRateConfiguration : EntityConfiguration<RoomRate, int>
{
    /// <inheritdoc />
    protected override string TableName => "room_rate";

    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<RoomRate> builder)
    {
        builder.Property(value => value.Size).HasConversion<int>().IsRequired();
        builder.Property(value => value.NightlyPrice).HasColumnType("numeric(12,2)").IsRequired();
        builder.Property(value => value.SetBy).HasMaxLength(RoomRate.MaxTextLength).IsRequired();
        builder.Property(value => value.SetAt).IsRequired();

        // One rate per room size; this index is also the safeguard when two changes arrive at the same moment.
        builder.HasIndex(value => value.Size)
            .IsUnique()
            .HasDatabaseName("ux_room_rate_size");
    }
}
