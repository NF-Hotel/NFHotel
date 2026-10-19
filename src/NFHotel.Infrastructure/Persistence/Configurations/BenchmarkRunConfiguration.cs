using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NFHotel.Domain.Entities;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="BenchmarkRun"/> to the table <c>benchmark_run</c> (ADR-0013).</summary>
public sealed class BenchmarkRunConfiguration : EntityConfiguration<BenchmarkRun, int>
{
    /// <inheritdoc />
    protected override string TableName => "benchmark_run";

    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<BenchmarkRun> builder)
    {
        builder.Property(run => run.RunId).IsRequired();
        builder.Property(run => run.StartedAt).IsRequired();
        builder.Property(run => run.FinishedAt).IsRequired();
        builder.Property(run => run.DatasetName).HasMaxLength(BenchmarkRun.MaxTextLength).IsRequired();
        builder.Property(run => run.DatasetHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(run => run.DatasetRows).IsRequired();
        builder.Property(run => run.PromptVersion).HasMaxLength(BenchmarkRun.MaxTextLength).IsRequired();
        builder.Property(run => run.Cpu).HasMaxLength(BenchmarkRun.MaxTextLength).IsRequired();
        builder.Property(run => run.CpuCores).IsRequired();
        builder.Property(run => run.MemoryMb).IsRequired();
        builder.Property(run => run.Gpu).HasMaxLength(BenchmarkRun.MaxTextLength);
        builder.Property(run => run.OperatingSystem).HasMaxLength(BenchmarkRun.MaxTextLength).IsRequired();
        builder.Property(run => run.BackendName).HasMaxLength(BenchmarkRun.MaxTextLength).IsRequired();
        builder.Property(run => run.BackendVersion).HasMaxLength(BenchmarkRun.MaxTextLength);

        // The run id of the contract is the key of idempotency. This index is also the safeguard when two
        // deliveries of the same run arrive at the same moment (ADR-0013).
        builder.HasIndex(run => run.RunId)
            .IsUnique()
            .HasDatabaseName("ux_benchmark_run_run_id");

        builder.HasIndex(run => run.StartedAt)
            .HasDatabaseName("ix_benchmark_run_started_at");

        builder.HasMany(run => run.Results)
            .WithOne()
            .HasForeignKey(result => result.BenchmarkRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(run => run.Results).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
