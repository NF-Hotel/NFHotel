using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NFHotel.Application.UseCases.Staff;
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

        builder.Property(member => member.FirstName)
            .HasMaxLength(StaffMember.MaxNameLength);

        builder.Property(member => member.LastName)
            .HasMaxLength(StaffMember.MaxNameLength);

        builder.Property(member => member.Email)
            .HasMaxLength(StaffMember.MaxEmailLength);

        builder.Property(member => member.Role)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(member => member.IsActive)
            .IsRequired();

        builder.Property(member => member.MustChangePassword)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(member => member.SecurityStamp)
            .HasDefaultValue(1)
            .IsRequired();

        // The user name is stored in lower case, so a plain unique index makes the name unique in any case.
        // It is also the race safeguard behind the check of the service.
        builder.HasIndex(member => member.UserName)
            .IsUnique()
            .HasDatabaseName("ux_staff_member_user_name");

        // The address is stored in lower case too. Accounts without one are left out, and the name lets the service tell a
        // taken address from a taken user name.
        builder.HasIndex(member => member.Email)
            .IsUnique()
            .HasFilter("email IS NOT NULL")
            .HasDatabaseName(StaffAccountService.EmailIndexName);

        builder.UseXminAsConcurrencyToken();
    }
}
