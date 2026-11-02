using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NFHotel.Domain.Entities;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="PriceChange"/> to the table <c>price_change</c> (MIL-051).</summary>
public sealed class PriceChangeConfiguration : EntityConfiguration<PriceChange, int>
{
    /// <inheritdoc />
    protected override string TableName => "price_change";

    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<PriceChange> builder)
    {
        builder.Property(value => value.Size).HasConversion<int>().IsRequired();
        builder.Property(value => value.Percentage).HasColumnType("numeric(7,2)").IsRequired();
        builder.Property(value => value.FirstDay).HasColumnType("date").IsRequired();
        builder.Property(value => value.LastDay).HasColumnType("date").IsRequired();
        builder.Property(value => value.State).HasConversion<int>().IsRequired();
        builder.Property(value => value.ApprovedBy).HasMaxLength(PriceChange.MaxTextLength).IsRequired();
        builder.Property(value => value.ApprovedAt).IsRequired();
        builder.Property(value => value.RevertedBy).HasMaxLength(PriceChange.MaxTextLength);
        builder.Property(value => value.RevertedAt);
        builder.Property(value => value.EndedOn).HasColumnType("date");

        // The effective price of a size on a day and the overlap check both read the changes of one size by first day.
        builder.HasIndex(value => new { value.Size, value.FirstDay })
            .HasDatabaseName("ix_price_change_size_first_day");
    }
}
