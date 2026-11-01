using NFHotel.Application.UseCases.Staff;

namespace NFHotel.Application.Tests.Security;

/// <summary>
/// Tests the access rights of the roles of the system.
/// </summary>
public class RolePermissionsTests
{
    #region Single permissions

    /// <summary>
    /// Checks that a manager is allowed to change prices.
    /// </summary>
    [Fact]
    public void HasPermission_ManagerAndManagePrices_IsTrue()
    {
        // Arrange  Prepare the role and the permission we want to test.
        string role = Roles.Manager;
        string permission = Permissions.ManagePrices;

        // Act  Check whether the role has the wanted permission.
        bool result = RolePermissions.HasPermission(role, permission);

        // Assert  The manager must be allowed.
        Assert.True(result);
    }

    /// <summary>
    /// Checks that a receptionist is not allowed to change prices.
    /// </summary>
    [Fact]
    public void HasPermission_ReceptionistAndManagePrices_IsFalse()
    {
        // Arrange  Prepare the role and the permission.
        string role = Roles.Receptionist;
        string permission = Permissions.ManagePrices;

        // Act  Check whether the role has the wanted permission.
        bool result = RolePermissions.HasPermission(role, permission);

        // Assert  The receptionist must not be allowed.
        Assert.False(result);
    }


    /// <summary>
    /// Checks that a guest is not allowed to see the passport data of hotel guests.
    /// </summary>
    [Fact]
    public void HasPermission_GuestAndGuestPassportData_IsFalse()
    {
        // Arrange  prepare the role and the permission.
        string role = Roles.Guest;
        string permission = Permissions.ViewGuestPassportData;

        // Act  check whether the role has the wanted permission.
        bool result = RolePermissions.HasPermission(role, permission);

        // Assert  the guest must not be allowed.
        Assert.False(result);
    }



    /// <summary>
    /// Checks that an unknown role gets no access to change prices.
    /// </summary>
    [Fact]
    public void HasPermission_UnknownRoleAndManagePrices_IsFalse()
    {
        // Arrange  create a role that is not in the RBAC matrix.
        string role = "UnknownRole";
        string permission = Permissions.ManagePrices;

        // Act  check whether the role has the wanted permission.
        bool result = RolePermissions.HasPermission(role, permission);

        // Assert  unknown roles must be denied access.
        Assert.False(result);
    }


    #endregion

    #region Whole matrix per role

    /// <summary>
    /// Verifies the permissions of the housekeeping role.
    /// </summary>
    [Theory]
    [InlineData(Permissions.ViewHousekeeping, true)]
    [InlineData(Permissions.UpdateHousekeepingStatus, true)]
    [InlineData(Permissions.ManagePrices, false)]
    [InlineData(Permissions.CreateBooking, false)]
    [InlineData(Permissions.ViewGuestPassportData, false)]
    [InlineData(Permissions.EditOwnBooking, false)]
    [InlineData(Permissions.EditOtherBookings, false)]
    [InlineData(Permissions.ViewOwnGuestData, false)]
    [InlineData(Permissions.ViewOtherGuestData, false)]
    [InlineData(Permissions.ViewOwnPassportData, false)]

    public void HasPermission_HousekeepingAndPermission_MatchesTheMatrix(
        string permission, bool expected)
    {
        // Arrange  the role is Housekeeping.
        string role = Roles.Housekeeping;

        // Act  check whether the role has the wanted permission.
        bool result = RolePermissions.HasPermission(role, permission);

        // Assert  compare the result with the expected access.
        Assert.Equal(expected, result);
    }



    /// <summary>
    /// Verifies the permissions of the Guest role.
    /// </summary>
    [Theory]
    [InlineData(Permissions.CreateBooking, true)]
    [InlineData(Permissions.EditOwnBooking, true)]
    [InlineData(Permissions.ViewOwnGuestData, true)]
    [InlineData(Permissions.EditOtherBookings, false)]
    [InlineData(Permissions.ViewOtherGuestData, false)]
    [InlineData(Permissions.ViewOwnPassportData, false)]
    [InlineData(Permissions.ViewGuestPassportData, false)]
    [InlineData(Permissions.ManagePrices, false)]
    [InlineData(Permissions.ViewHousekeeping, false)]
    [InlineData(Permissions.UpdateHousekeepingStatus, false)]
    public void HasPermission_GuestAndPermission_MatchesTheMatrix(
        string permission, bool expected)
    {
        // Arrange  the role is Guest.
        string role = Roles.Guest;

        // Act  check whether the role has the wanted permission.
        bool result = RolePermissions.HasPermission(role, permission);

        // Assert  compare with the expected access.
        Assert.Equal(expected, result);
    }


    /// <summary>
    /// Verifies the permissions of the Receptionist role.
    /// </summary>
    [Theory]
    [InlineData(Permissions.CreateBooking, true)]
    [InlineData(Permissions.EditOwnBooking, true)]
    [InlineData(Permissions.EditOtherBookings, true)]
    [InlineData(Permissions.ViewOwnGuestData, true)]
    [InlineData(Permissions.ViewOtherGuestData, true)]
    [InlineData(Permissions.ViewOwnPassportData, false)]
    [InlineData(Permissions.ViewGuestPassportData, true)]
    [InlineData(Permissions.ManagePrices, false)]
    [InlineData(Permissions.ViewHousekeeping, true)]
    [InlineData(Permissions.UpdateHousekeepingStatus, false)]
    public void HasPermission_ReceptionistAndPermission_MatchesTheMatrix(
        string permission, bool expected)
    {
        // Arrange  the role is Receptionist.
        string role = Roles.Receptionist;

        // Act  check whether the role has the wanted permission.
        bool result = RolePermissions.HasPermission(role, permission);

        // Assert  compare with the expected access.
        Assert.Equal(expected, result);
    }



    /// <summary>
    /// Verifies the permissions of the Manager role.
    /// </summary>
    [Theory]
    [InlineData(Permissions.CreateBooking, true)]
    [InlineData(Permissions.EditOwnBooking, true)]
    [InlineData(Permissions.EditOtherBookings, true)]
    [InlineData(Permissions.ViewOwnGuestData, true)]
    [InlineData(Permissions.ViewOtherGuestData, true)]
    [InlineData(Permissions.ViewOwnPassportData, false)]
    [InlineData(Permissions.ViewGuestPassportData, true)]
    [InlineData(Permissions.ManagePrices, true)]
    [InlineData(Permissions.ViewHousekeeping, true)]
    [InlineData(Permissions.UpdateHousekeepingStatus, true)]
    public void HasPermission_ManagerAndPermission_MatchesTheMatrix(
        string permission, bool expected)
    {
        // Arrange the role is Manager.
        string role = Roles.Manager;

        // Act  check whether the role has the wanted permission.
        bool result = RolePermissions.HasPermission(role, permission);

        // Assert  compare with the expected access.
        Assert.Equal(expected, result);
    }

    #endregion
}
