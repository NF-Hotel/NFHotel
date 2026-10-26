using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NFHotel.Domain.Entities;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="BenchmarkRequest"/> to the table <c>benchmark_request</c> (ADR-0031).</summary>
public sealed class BenchmarkRequestConfiguration : EntityConfiguration<BenchmarkRequest, int>
{
    /// <inheritdoc />
    protected override string TableName => "benchmark_request";

    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<BenchmarkRequest> builder)
    {
        builder.Property(request => request.RunId).IsRequired();

        // The models are a text array; the entity exposes them read-only and keeps them in a field.
        builder.Ignore(request => request.Models);
        builder.PrimitiveCollection<List<string>>("_models")
            .HasColumnName("models")
            .IsRequired();

        builder.Property(request => request.HotelAnalysis).IsRequired();
        builder.Property(request => request.DeterministicTasks).IsRequired();
        builder.Property(request => request.Runs).IsRequired();
        builder.Property(request => request.RequestedBy).HasMaxLength(BenchmarkRequest.MaxTextLength).IsRequired();
        builder.Property(request => request.RequestedAt).IsRequired();
        builder.Property(request => request.State).HasMaxLength(20).IsRequired();
        builder.Property(request => request.Error).HasMaxLength(BenchmarkRequest.MaxErrorLength);
        builder.Property(request => request.UpdatedAt).IsRequired();
        builder.Ignore(request => request.IsFinal);

        // One record per run of the service; this index is also the safeguard when the same run is recorded twice at once.
        builder.HasIndex(request => request.RunId)
            .IsUnique()
            .HasDatabaseName("ux_benchmark_request_run_id");

        builder.HasIndex(request => request.RequestedAt)
            .HasDatabaseName("ix_benchmark_request_requested_at");
    }
}
