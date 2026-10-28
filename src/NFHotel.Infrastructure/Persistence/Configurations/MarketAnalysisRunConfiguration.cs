using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NFHotel.Domain.Entities;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="MarketAnalysisRun"/> to the table <c>market_analysis_run</c> (MIL-026).</summary>
public sealed class MarketAnalysisRunConfiguration : EntityConfiguration<MarketAnalysisRun, int>
{
    /// <inheritdoc />
    protected override string TableName => "market_analysis_run";

    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<MarketAnalysisRun> builder)
    {
        builder.Property(run => run.RunId).IsRequired();
        builder.Property(run => run.SchemaVersion).IsRequired();
        builder.Property(run => run.StartedAt).IsRequired();
        builder.Property(run => run.FinishedAt).IsRequired();
        builder.Property(run => run.Provider).HasMaxLength(MarketAnalysisRun.MaxTextLength);
        builder.Property(run => run.Model).HasMaxLength(MarketAnalysisRun.MaxTextLength);
        builder.Property(run => run.ContentJson).HasColumnType("text").IsRequired();

        // The run id of the service is the key of idempotency. This index is also the safeguard when two polls try to
        // store the same run at the same moment.
        builder.HasIndex(run => run.RunId)
            .IsUnique()
            .HasDatabaseName("ux_market_analysis_run_run_id");

        builder.HasIndex(run => run.FinishedAt)
            .HasDatabaseName("ix_market_analysis_run_finished_at");
    }
}
