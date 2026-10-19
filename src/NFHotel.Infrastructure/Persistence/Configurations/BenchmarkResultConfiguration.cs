using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NFHotel.Domain.Entities;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="BenchmarkResult"/> to the table <c>benchmark_result</c> (ADR-0013).</summary>
public sealed class BenchmarkResultConfiguration : EntityConfiguration<BenchmarkResult, int>
{
    /// <inheritdoc />
    protected override string TableName => "benchmark_result";

    /// <inheritdoc />
    protected override void ConfigureTable(TableBuilder<BenchmarkResult> table)
    {
        table.HasCheckConstraint("ck_benchmark_result_total_seconds_non_negative", "total_seconds >= 0");
        table.HasCheckConstraint("ck_benchmark_result_completion_tokens_non_negative", "completion_tokens >= 0");
    }

    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<BenchmarkResult> builder)
    {
        builder.Property(result => result.BenchmarkRunId).IsRequired();
        builder.Property(result => result.Timestamp).IsRequired();
        builder.Property(result => result.Backend).HasMaxLength(BenchmarkResult.MaxNameLength).IsRequired();
        builder.Property(result => result.Model).HasMaxLength(BenchmarkResult.MaxNameLength).IsRequired();
        builder.Property(result => result.AnalysisType).HasMaxLength(BenchmarkResult.MaxNameLength).IsRequired();
        builder.Property(result => result.TaskId).HasMaxLength(BenchmarkResult.MaxNameLength);
        builder.Property(result => result.Category).HasMaxLength(BenchmarkResult.MaxNameLength);
        builder.Property(result => result.FinishReason).HasMaxLength(BenchmarkResult.MaxFinishReasonLength);
        builder.Property(result => result.RunNumber).IsRequired();
        builder.Property(result => result.Cold).IsRequired();
        builder.Property(result => result.TotalSeconds).IsRequired();
        builder.Property(result => result.CompletionTokens).IsRequired();
        builder.Property(result => result.TokensEstimated).IsRequired();

        // The answer, the error and the validation text can be long: text columns, never read by the list.
        builder.Property(result => result.Output).HasColumnType("text");
        builder.Property(result => result.Error).HasColumnType("text");
        builder.Property(result => result.ValidationError).HasColumnType("text");

        builder.HasIndex(result => result.BenchmarkRunId)
            .HasDatabaseName("ix_benchmark_result_benchmark_run_id");
    }
}
