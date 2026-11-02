using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Tests.Security;

/// <summary>The permission that sets the nightly base rates (MIL-055): only the Manager holds it.</summary>
public sealed class ManagePricesPermissionTests
{
    /// <summary>Positive: the Manager holds the permission to manage prices.</summary>
    [Fact]
    public void HasPermission_ManagerAndManagePrices_IsTrue() =>
        Assert.True(RolePermissions.HasPermission(StaffRole.Manager, Permissions.ManagePrices));

    /// <summary>Negative: the Receptionist and Housekeeping do not hold it.</summary>
    [Theory]
    [InlineData(StaffRole.Receptionist)]
    [InlineData(StaffRole.Housekeeping)]
    public void HasPermission_OtherStaffRoleAndManagePrices_IsFalse(StaffRole role) =>
        Assert.False(RolePermissions.HasPermission(role, Permissions.ManagePrices));

    /// <summary>Positive: every staff role that reads the rates holds the permission to see rooms.</summary>
    [Theory]
    [InlineData(StaffRole.Manager)]
    [InlineData(StaffRole.Receptionist)]
    [InlineData(StaffRole.Housekeeping)]
    public void HasPermission_EveryStaffRoleAndRoomsView_IsTrue(StaffRole role) =>
        Assert.True(RolePermissions.HasPermission(role, Permissions.RoomsView));
}
