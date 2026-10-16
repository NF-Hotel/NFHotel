using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NFHotel.Domain.Entities;

namespace NFHotel.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="StaffMember"/> to the table <c>staff_member</c> (ADR-0005).</summary>
public sealed class StaffConfiguration : EntityConfiguration<StaffMember, int>
{
    /// <inheritdoc />
    protected override string TableName => "staff_member";

    /// <inheritdoc />
    protected override void ConfigureEntity(EntityTypeBuilder<StaffMember> builder)
    {
        builder.Property(member => member.UserName)
            .HasMaxLength(StaffMember.MaxUserNameLength)
            .IsRequired();

        builder.Property(member => member.PasswordHash)
            .HasMaxLength(StaffMember.MaxPasswordHashLength)
            .IsRequired();

        builder.Property(member => member.Role)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(member => member.IsActive)
            .IsRequired();

        // The user name is stored in lower case, so a plain unique index makes the name unique in any case.
        // It is also the race safeguard behind the check of the service.
        builder.HasIndex(member => member.UserName)
            .IsUnique()
            .HasDatabaseName("ux_staff_member_user_name");

        builder.UseXminAsConcurrencyToken();
    }
}