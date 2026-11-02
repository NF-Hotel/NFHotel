using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="PricingSetting"/> to the table <c>pricing_settings</c> (MIL-055).</summary>
public sealed class PricingSettingConfiguration : EntityConfiguration<PricingSetting, int>
{
    private const char SizeSeparator = ',';

    /// <inheritdoc />
    protected override string TableName => "pricing_settings";

    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<PricingSetting> builder)
    {
        builder.Property(value => value.Slot).IsRequired();
        builder.Property(value => value.SurchargePercent).HasColumnType("numeric(5,2)").IsRequired();
        builder.Property(value => value.DiscountPercent).HasColumnType("numeric(5,2)").IsRequired();
        builder.Property(value => value.HighThresholdPercent).HasColumnType("numeric(5,2)").IsRequired();
        builder.Property(value => value.LowThresholdPercent).HasColumnType("numeric(5,2)").IsRequired();
        builder.Property(value => value.MaxChangePercent).HasColumnType("numeric(5,2)").IsRequired();
        builder.Property(value => value.MinimumNightlyPrice).HasColumnType("numeric(12,2)").IsRequired();
        builder.Property(value => value.SetBy).HasMaxLength(PricingSetting.MaxTextLength).IsRequired();
        builder.Property(value => value.SetAt).IsRequired();

        // The sizes are kept as the comma separated names of the enum, for example "Single,Double"; the list is always replaced, never changed in place.
        // The `!` below: the column is required, so EF never compares a null list, and an expression tree cannot hold a null check pattern.
        builder.Property(value => value.DiscountSizes)
            .HasConversion(
                sizes => string.Join(SizeSeparator, sizes.Select(size => size.ToString())),
                text => ParseSizes(text),
                new ValueComparer<IReadOnlyList<RoomSize>>(
                    (left, right) => left!.SequenceEqual(right!),
                    sizes => sizes.Aggregate(0, (hash, size) => HashCode.Combine(hash, (int)size)),
                    sizes => sizes.ToArray()))
            .HasMaxLength(64)
            .IsRequired();

        // At most one row: the slot is always 1 and unique, and the table rejects any other value.
        builder.HasIndex(value => value.Slot)
            .IsUnique()
            .HasDatabaseName("ux_pricing_settings_slot");
    }

    /// <inheritdoc />
    protected override void ConfigureTable(TableBuilder<PricingSetting> table) =>
        table.HasCheckConstraint("ck_pricing_settings_slot", $"slot = {PricingSetting.SingletonSlot}");

    private static IReadOnlyList<RoomSize> ParseSizes(string text) =>
        [.. text.Split(SizeSeparator, StringSplitOptions.RemoveEmptyEntries).Select(Enum.Parse<RoomSize>)];
}
