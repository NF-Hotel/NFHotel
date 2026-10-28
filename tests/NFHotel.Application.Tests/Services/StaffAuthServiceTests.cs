using NFHotel.Application.Dto.Staff;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

using NSubstitute;

namespace NFHotel.Application.Tests.Services;

/// <summary>The sign-in use case (ADR-0005) with in-memory accounts, a fake hasher and a substitute throttle.</summary>
public sealed class StaffAuthServiceTests
{
    private const string Password = "correct horse battery staple";

    #region Success

    /// <summary>Positive: the right password gives the session with the token, the expiry, the stored name and the role.</summary>
    [Fact]
    public async Task SignInAsync_RightPassword_ReturnsTheSession()
    {
        // Arrange
        var (service, world, _) = Build();
        Add(world, "rita", StaffRole.Receptionist);

        // Act
        var result = await service.SignInAsync(new SignInRequest("rita", Password), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("rita", result.Value.UserName);
        Assert.Equal(StaffRole.Receptionist, result.Value.Role);
        Assert.Equal("token-1-rita-Receptionist", result.Value.Token);
    }

    /// <summary>Equivalence: the name is found in any case and with spaces around it.</summary>
    [Theory]
    [InlineData("RITA")]
    [InlineData("  Rita ")]
    public async Task SignInAsync_NameInOtherCase_ReturnsTheSession(string typed)
    {
        // Arrange
        var (service, world, _) = Build();
        Add(world, "rita", StaffRole.Manager);

        // Act
        var result = await service.SignInAsync(new SignInRequest(typed, Password), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
    }

    /// <summary>State transition: a success forgets the earlier failures of the name.</summary>
    [Fact]
    public async Task SignInAsync_Success_ResetsTheThrottleOfTheName()
    {
        // Arrange
        var (service, world, throttle) = Build();
        Add(world, "rita", StaffRole.Manager);

        // Act
        await service.SignInAsync(new SignInRequest("rita", Password), TestContext.Current.CancellationToken);

        // Assert
        throttle.Received(1).Reset("rita");
        throttle.DidNotReceive().RecordFailure(Arg.Any<string>());
    }

    #endregion

    #region Refusal

    /// <summary>Negative: a wrong password is refused with the credentials code and counted as a failure.</summary>
    [Fact]
    public async Task SignInAsync_WrongPassword_ReturnsInvalidCredentialsAndCountsIt()
    {
        // Arrange
        var (service, world, throttle) = Build();
        Add(world, "rita", StaffRole.Manager);

        // Act
        var result = await service.SignInAsync(new SignInRequest("rita", "not the password"), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Staff.InvalidCredentials, result.FirstError?.Code);
        throttle.Received(1).RecordFailure("rita");
    }

    /// <summary>Equivalence: an unknown user, a wrong password and an inactive account give the same code.</summary>
    [Fact]
    public async Task SignInAsync_UnknownWrongAndInactive_GiveTheSameCode()
    {
        // Arrange
        var (service, world, _) = Build();
        Add(world, "rita", StaffRole.Manager);
        Add(world, "ivan", StaffRole.Manager).Deactivate();

        // Act
        var unknown = await service.SignInAsync(new SignInRequest("nobody", Password), TestContext.Current.CancellationToken);
        var wrong = await service.SignInAsync(new SignInRequest("rita", "wrong"), TestContext.Current.CancellationToken);
        var inactive = await service.SignInAsync(new SignInRequest("ivan", Password), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Staff.InvalidCredentials, unknown.FirstError?.Code);
        Assert.Equal(ErrorCodes.Staff.InvalidCredentials, wrong.FirstError?.Code);
        Assert.Equal(ErrorCodes.Staff.InvalidCredentials, inactive.FirstError?.Code);
    }

    /// <summary>The password is checked for an unknown user too, so the time does not give the user away.</summary>
    [Fact]
    public async Task SignInAsync_UnknownUser_StillChecksAPasswordHash()
    {
        // Arrange
        var world = new ServiceWorld();
        var hasher = Substitute.For<IPasswordHasher>();
        var service = world.StaffAuthService(hasher);

        // Act
        await service.SignInAsync(new SignInRequest("nobody", Password), TestContext.Current.CancellationToken);

        // Assert
        hasher.Received(1).Verify(null, Password);
    }

    /// <summary>Negative: a blank name or an empty password is refused like any other wrong credentials.</summary>
    [Theory]
    [InlineData("", Password)]
    [InlineData("   ", Password)]
    [InlineData("rita", "")]
    public async Task SignInAsync_BlankNameOrPassword_ReturnsInvalidCredentials(string name, string password)
    {
        // Arrange
        var (service, world, _) = Build();
        Add(world, "rita", StaffRole.Manager);

        // Act
        var result = await service.SignInAsync(new SignInRequest(name, password), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Staff.InvalidCredentials, result.FirstError?.Code);
    }

    /// <summary>Boundary: a password above the longest length is refused without looking the account up.</summary>
    [Fact]
    public async Task SignInAsync_PasswordOverTheLimit_IsRefusedWithoutLookingTheAccountUp()
    {
        // Arrange
        var world = new ServiceWorld();
        Add(world, "rita", StaffRole.Manager);
        var hasher = Substitute.For<IPasswordHasher>();
        var service = world.StaffAuthService(hasher);
        var tooLong = new string('x', StaffAuthService.MaxPasswordLength + 1);

        // Act
        var result = await service.SignInAsync(new SignInRequest("rita", tooLong), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Staff.InvalidCredentials, result.FirstError?.Code);
        hasher.Received(1).Verify(null, tooLong);
    }

    #endregion

    #region Throttle

    /// <summary>Negative: a name that has used up its attempts is refused with the throttle code, even with the right password, and nothing is checked.</summary>
    [Fact]
    public async Task SignInAsync_BlockedName_ReturnsTooManyAttemptsWithoutCheckingThePassword()
    {
        // Arrange
        var world = new ServiceWorld();
        Add(world, "rita", StaffRole.Manager);
        var hasher = Substitute.For<IPasswordHasher>();
        var throttle = Substitute.For<ILoginThrottle>();
        throttle.IsBlocked("rita").Returns(true);
        var service = world.StaffAuthService(hasher, throttle: throttle);

        // Act
        var result = await service.SignInAsync(new SignInRequest("rita", Password), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Staff.TooManyAttempts, result.FirstError?.Code);
        hasher.DidNotReceiveWithAnyArgs().Verify(default, default!);
        throttle.DidNotReceive().RecordFailure(Arg.Any<string>());
    }

    /// <summary>Negative: a missing request is a programming error.</summary>
    [Fact]
    public async Task SignInAsync_NullRequest_Throws()
    {
        // Arrange
        var (service, _, _) = Build();

        // Act and assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.SignInAsync(null!, TestContext.Current.CancellationToken));
    }

    #endregion

    #region Helpers

    private static (StaffAuthService Service, ServiceWorld World, ILoginThrottle Throttle) Build()
    {
        var world = new ServiceWorld();
        var throttle = Substitute.For<ILoginThrottle>();
        return (world.StaffAuthService(throttle: throttle), world, throttle);
    }

    private static StaffMember Add(ServiceWorld world, string name, StaffRole role)
    {
        var member = StaffMember.Create(name, new FakePasswordHasher().Hash(Password), role);
        world.Staff.Add(member);
        return member;
    }

    #endregion
}
