using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NFHotel.Domain.Entities;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="StaffAuditEvent"/> to the table <c>staff_audit_event</c> (ADR-0015).</summary>
public sealed class StaffAuditEventConfiguration : EntityConfiguration<StaffAuditEvent, int>
{
    /// <inheritdoc />
    protected override string TableName => "staff_audit_event";

    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<StaffAuditEvent> builder)
    {
        builder.Property(auditEvent => auditEvent.OccurredAt).IsRequired();
        builder.Property(auditEvent => auditEvent.ActorId);
        builder.Property(auditEvent => auditEvent.ActorName).HasMaxLength(StaffAuditEvent.MaxTextLength);
        builder.Property(auditEvent => auditEvent.Action).HasConversion<int>().IsRequired();
        builder.Property(auditEvent => auditEvent.TargetId);
        builder.Property(auditEvent => auditEvent.TargetName).HasMaxLength(StaffAuditEvent.MaxTextLength);
        builder.Property(auditEvent => auditEvent.Outcome).HasMaxLength(StaffAuditEvent.MaxTextLength).IsRequired();
        builder.Property(auditEvent => auditEvent.RemoteAddress).HasMaxLength(StaffAuditEvent.MaxTextLength);

        // The audit page reads newest first; the key breaks ties between events of the same moment.
        builder.HasIndex(auditEvent => auditEvent.OccurredAt)
            .HasDatabaseName("ix_staff_audit_event_occurred_at");
    }
}
