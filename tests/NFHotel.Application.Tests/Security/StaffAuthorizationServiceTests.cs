using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;

using NSubstitute;

namespace NFHotel.Application.Tests.Security;

/// <summary>
/// Tests authorization checks for staff members.
/// </summary>
public sealed class StaffAuthorizationServiceTests
{

    // Tests that authorization is denied when no staff ID is available.
    [Fact]
    public async Task HasPermissionAsync_NoStaffId_ReturnsFalse()
    {
        // Arrange
        var context = Substitute.For<IStaffCallContext>();
        var staff = Substitute.For<IStaffRepository>();

        context.StaffId.Returns((int?)null);

        var service = new StaffAuthorizationService(context, staff);

        // Act
        var result = await service.HasPermissionAsync(
            Permissions.StaffManage,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    // Tests that an inactive staff member is denied permission.
    [Fact]
    public async Task HasPermissionAsync_InactiveStaff_ReturnsFalse()
    {
        // Arrange

        // Creates a mock implementation of IStaffCallContext using NSubstitute.
        var context = Substitute.For<IStaffCallContext>();
        var staff = Substitute.For<IStaffRepository>();

        // Creates a test staff member with the Manager role.
        var member = StaffMember.Create(
            "existing",
            "hash",
            StaffRole.Manager);

        // Deactivates the staff member to simulate an inactive state.
        member.Deactivate();

        context.StaffId.Returns(1);


        // Configures the mocked repository to return the inactive staff member
        // when GetByIdAsync is called with ID 1.
        // Arg.Any<CancellationToken>() accepts any cancellation token.
        staff.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(member);


        // Creates the authorization service using the mocked dependencies.
        var service = new StaffAuthorizationService(context, staff);

        // Act
        var result = await service.HasPermissionAsync(
            Permissions.StaffManage,
            TestContext.Current.CancellationToken);

        // Assert

        // Verifies that the method returns false.
        // An inactive staff member must not have access,
        // even if their role normally has the requested permission.
        Assert.False(result);
    }


    [Fact]
    public async Task HasPermissionAsync_StaffWithoutPermission_ReturnsFalse()
    {
        // Arrange
        var context = Substitute.For<IStaffCallContext>();
        var staff = Substitute.For<IStaffRepository>();

        // Creates an active staff member with the Receptionist role.
        var member = StaffMember.Create(
            "receptionist",
            "hash",
            StaffRole.Receptionist);

        context.StaffId.Returns(1);

        // Configures the repository to return the staff member.
        staff.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(member);

        var service = new StaffAuthorizationService(context, staff);

        // Act
        var result = await service.HasPermissionAsync(
            Permissions.StaffManage,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }


    // Tests that an active Manager with the required permission is granted access.
    [Fact]
    public async Task HasPermissionAsync_StaffWithPermission_ReturnsTrue()
    {
        // Arrange
        var context = Substitute.For<IStaffCallContext>();
        var staff = Substitute.For<IStaffRepository>();

        // Creates an active staff member with the Manager role.
        var member = StaffMember.Create(
            "manager",
            "hash",
            StaffRole.Manager);

        context.StaffId.Returns(1);

        // Configures the repository to return the Manager.
        staff.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(member);

        var service = new StaffAuthorizationService(context, staff);

        // Act
        var result = await service.HasPermissionAsync(
            Permissions.StaffManage,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
    }

}
