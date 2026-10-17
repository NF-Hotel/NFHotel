using System;


using NFHotel.Application.Security;

namespace NFHotel.Application.Tests.Security;

/// <summary>
/// Tester adgangsrettighederne for systemets roller.
/// </summary>
public class RolePermissionsTests
{
    /// <summary>
    /// Kontrollerer at manager har tilladelse til at ændre priser.
    /// </summary>
    [Fact]
    public void Manager_CanManagePrices()
    {
        // Arrange  Forbered rollen og den permission, vi vil teste.
        string role = Roles.Manager;
        string permission = Permissions.ManagePrices;

        // Act  Kontroller om rollen har den ønskede permission.
        bool result = RolePermissions.HasPermission(role, permission);

        // Assert  Manager skal have tilladelse.
        Assert.True(result);
    }

    /// <summary>
    /// Kontrollerer at Receptionist ikke har tilladelse til at ændre priser.
    /// </summary>
    [Fact]
    public void Receptionist_CannotManagePrices()
    {
        // Arrange  Forbered rolle og permission.
        string role = Roles.Receptionist;
        string permission = Permissions.ManagePrices;

        // Act  Kontroller om rollen har den ønskede permission.
        bool result = RolePermissions.HasPermission(role, permission);

        // Assert  Receptionist skal ikke have tilladelse.
        Assert.False(result);
    }


    /// <summary>
    /// Kontrollerer at guest ikke har tilladelse til at se hotelgæsters pasdata.
    /// </summary>
    [Fact]
    public void Guest_CannotViewGuestPassportData()
    {
        // Arrange  forbered rolle og permission.
        string role = Roles.Guest;
        string permission = Permissions.ViewGuestPassportData;

        // Act  kontroller om rollen har den ønskede permission.
        bool result = RolePermissions.HasPermission(role, permission);

        // Assert  guest skal ikke have tilladelse.
        Assert.False(result);
    }



    /// <summary>
    /// Kontrollerer at en ukendt rolle ikke får adgang til at ændre priser.
    /// </summary>
    [Fact]
    public void UnknownRole_CannotManagePrices()
    {
        // Arrange  opret en rolle, der ikke findes i RBAC-matrixen.
        string role = "UnknownRole";
        string permission = Permissions.ManagePrices;

        // Act  kontroller om rollen har den ønskede permission.
        bool result = RolePermissions.HasPermission(role, permission);

        // Assert ukendte roller skal nægtes adgang.
        Assert.False(result);
    }


    /// <summary>
    /// Kontrollerer housekeeping-rollens adgangsrettigheder.
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

    public void Housekeeping_HasCorrectPermissions(
        string permission, bool expected)
    {
        // Arrange  rollen er Housekeeping.
        string role = Roles.Housekeeping;

        // Act  kontroller om rollen har den ønskede permission.
        bool result = RolePermissions.HasPermission(role, permission);

        // Assert  sammenlign resultatet med den forventede adgang.
        Assert.Equal(expected, result);
    }



    /// <summary>
    /// Kontrollerer Guest-rollens adgangsrettigheder.
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
    public void Guest_HasCorrectPermissions(
        string permission, bool expected)
    {
        // Arrange  rollen er Guest.
        string role = Roles.Guest;

        // Act  kontroller om rollen har den ønskede permission.
        bool result = RolePermissions.HasPermission(role, permission);

        // Assert  sammenlign med den forventede adgang.
        Assert.Equal(expected, result);
    }


    /// <summary>
    /// Kontrollerer Receptionist-rollens adgangsrettigheder.
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
    public void Receptionist_HasCorrectPermissions(
        string permission, bool expected)
    {
        // Arrange  rollen er Receptionist.
        string role = Roles.Receptionist;

        // Act  kontroller om rollen har den ønskede permission.
        bool result = RolePermissions.HasPermission(role, permission);

        // Assert  sammenlign med den forventede adgang.
        Assert.Equal(expected, result);
    }



    /// <summary>
    /// Kontrollerer Manager-rollens adgangsrettigheder.
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
    public void Manager_HasCorrectPermissions(
        string permission, bool expected)
    {
        // Arrange rollen er Manager.
        string role = Roles.Manager;

        // Act  kontroller om rollen har den ønskede permission.
        bool result = RolePermissions.HasPermission(role, permission);

        // Assert  sammenlign med den forventede adgang.
        Assert.Equal(expected, result);
    }


}
