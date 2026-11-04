using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NFHotel.Domain.Entities;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="Shift"/> to the table <c>shift</c> (MIL-060).</summary>
public sealed class ShiftConfiguration : EntityConfiguration<Shift, int>
{
    /// <inheritdoc />
    protected override string TableName => "shift";

    /// <inheritdoc />
    protected override void ConfigureTable(TableBuilder<Shift> table) =>
        table.HasCheckConstraint("ck_shift_ends_after_start", "ends_at > starts_at");

    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<Shift> builder)
    {
        builder.Property(shift => shift.AssigneeId).HasColumnName("assignee_staff_member_id");
        builder.Property(shift => shift.StartsAt).IsRequired();
        builder.Property(shift => shift.EndsAt).IsRequired();
        builder.Property(shift => shift.Role).HasConversion<int>().IsRequired();

        // A deleted account leaves its shifts open for the Manager to fill.
        builder.HasOne<StaffMember>().WithMany().HasForeignKey(shift => shift.AssigneeId).OnDelete(DeleteBehavior.SetNull);

        // The plan of a period, and the shifts of one member that could overlap.
        builder.HasIndex(shift => shift.StartsAt).HasDatabaseName("ix_shift_starts_at");
        builder.HasIndex(shift => new { shift.AssigneeId, shift.StartsAt }).HasDatabaseName("ix_shift_assignee_starts_at");

        // Two approvals that hand the same shift over at the same moment: the second save fails.
        builder.UseXminAsConcurrencyToken();
    }
}
