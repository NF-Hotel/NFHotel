using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NFHotel.Domain.Entities;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="RoomAssignment"/> to the table <c>room_assignment</c> (MIL-060).</summary>
public sealed class RoomAssignmentConfiguration : EntityConfiguration<RoomAssignment, int>
{
    /// <inheritdoc />
    protected override string TableName => "room_assignment";

    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<RoomAssignment> builder)
    {
        builder.Property(assignment => assignment.StaffId).HasColumnName("staff_member_id");

        // A deleted room or account takes its assignments with it; the room is then without a housekeeper.
        builder.HasOne<Room>().WithMany().HasForeignKey(assignment => assignment.RoomId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<StaffMember>().WithMany().HasForeignKey(assignment => assignment.StaffId).OnDelete(DeleteBehavior.Cascade);

        // One housekeeper per room; the index is also the race safeguard of two Managers assigning at once.
        builder.HasIndex(assignment => assignment.RoomId).IsUnique().HasDatabaseName("ux_room_assignment_room_id");
        builder.HasIndex(assignment => assignment.StaffId).HasDatabaseName("ix_room_assignment_staff_id");

        // Two trades that move the same room at the same moment: the second save fails instead of overwriting.
        builder.UseXminAsConcurrencyToken();
    }
}
