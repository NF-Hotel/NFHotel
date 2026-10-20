using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NFHotel.Domain.Entities;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="AiDefault"/> to the table <c>ai_default</c> (ADR-0014).</summary>
public sealed class AiDefaultConfiguration : EntityConfiguration<AiDefault, int>
{
    /// <inheritdoc />
    protected override string TableName => "ai_default";

    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<AiDefault> builder)
    {
        builder.Property(value => value.Purpose).HasMaxLength(AiDefault.MaxTextLength).IsRequired();
        builder.Property(value => value.Model).HasMaxLength(AiDefault.MaxTextLength).IsRequired();
        builder.Property(value => value.SetBy).HasMaxLength(AiDefault.MaxTextLength).IsRequired();
        builder.Property(value => value.SetAt).IsRequired();
        builder.Property(value => value.SourceRunId);

        // One default per purpose; this index is also the safeguard when two changes arrive at the same moment.
        builder.HasIndex(value => value.Purpose)
            .IsUnique()
            .HasDatabaseName("ux_ai_default_purpose");
    }
}
