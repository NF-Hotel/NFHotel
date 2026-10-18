using System.Net;
using System.Net.Http.Json;

using NFHotel.Api.Contracts;
using NFHotel.Api.Startup;
using NFHotel.Application.Dto.Staff;
using NFHotel.Application.Errors;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Api.Tests;

/// <summary>What the API does before it serves a call: the first Manager from the environment file and the create-staff command (ADR-0005).</summary>
public sealed class ApiStartupTests
{
    private const string Password = "correct horse battery staple";

    #region The first Manager

    /// <summary>Positive: with an empty table and both settings, the API starts with a Manager who can sign in.</summary>
    [Fact]
    public async Task Start_EmptyTableAndInitialManager_CreatesAManagerWhoCanSignIn()
    {
        // Arrange
        using var factory = new FirstManagerFactory("boss", Password);

        // Act
        using var client = factory.CreateServiceClient();
        using var response = await client.PostAsJsonAsync("/api/auth/login", new SignInRequest("boss", Password), ApiJson.Options, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(StaffRole.Manager, Assert.Single(factory.World.Staff.Items).Role);
    }

    /// <summary>Idempotence: when an account exists already, a start changes nothing.</summary>
    [Fact]
    public async Task Start_AccountExists_ChangesNothing()
    {
        // Arrange
        using var factory = new FirstManagerFactory("boss", Password);
        factory.World.Staff.Add(StaffMember.Create("existing", "hash", StaffRole.Housekeeping));

        // Act
        using var client = factory.CreateServiceClient();
        await client.GetAsync("/health", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("existing", Assert.Single(factory.World.Staff.Items).UserName);
    }

    /// <summary>Negative: a short password for the first Manager stops the start, and the message names the code and not the password.</summary>
    [Fact]
    public void Start_InitialManagerWithAShortPassword_StopsWithoutRevealingThePassword()
    {
        // Arrange
        using var factory = new FirstManagerFactory("boss", "short-pw");

        // Act
        var error = Assert.ThrowsAny<Exception>(() => factory.CreateServiceClient());

        // Assert
        var text = error.ToString();
        Assert.Contains(ErrorCodes.Staff.PasswordInvalid, text, StringComparison.Ordinal);
        Assert.DoesNotContain("short-pw", text, StringComparison.Ordinal);
    }

    /// <summary>Equivalence: with no initial manager settings nothing is created.</summary>
    [Fact]
    public async Task Start_NoInitialManagerSettings_CreatesNothing()
    {
        // Arrange
        using var factory = new ApiFactory();

        // Act
        using var client = factory.CreateServiceClient();
        await client.GetAsync("/health", TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(factory.World.Staff.Items);
    }

    #endregion

    #region The create-staff command

    /// <summary>Equivalence: only the exact command name is the create-staff command.</summary>
    [Theory]
    [InlineData("create-staff", true)]
    [InlineData("Create-Staff", false)]
    [InlineData("serve", false)]
    public void IsCreateStaff_FirstArgument_MatchesTheExactName(string first, bool expected)
    {
        // Arrange and act
        var matches = ApiStartup.IsCreateStaff([first, "x", "Manager"]);

        // Assert
        Assert.Equal(expected, matches);
    }

    /// <summary>Equivalence: no arguments is not the command.</summary>
    [Fact]
    public void IsCreateStaff_NoArguments_IsFalse()
    {
        // Arrange and act
        var matches = ApiStartup.IsCreateStaff([]);

        // Assert
        Assert.False(matches);
    }

    /// <summary>Positive: the command creates the account with the given role and the password it was handed.</summary>
    [Fact]
    public async Task RunCreateStaffAsync_ValidArguments_CreatesTheAccountAndReturnsZero()
    {
        // Arrange
        using var factory = new ApiFactory();

        // Act
        var code = await ApiStartup.RunCreateStaffAsync(factory.Services, ["create-staff", "Hilda", "housekeeping"], Password, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(0, code);
        var stored = Assert.Single(factory.World.Staff.Items);
        Assert.Equal("hilda", stored.UserName);
        Assert.Equal(StaffRole.Housekeeping, stored.Role);
    }

    /// <summary>Negative: a wrong command line (missing role, unknown role, numeric role, missing password) returns 2 and creates nothing.</summary>
    [Theory]
    [InlineData(new[] { "create-staff", "hilda" }, Password)]
    [InlineData(new[] { "create-staff", "hilda", "Director" }, Password)]
    [InlineData(new[] { "create-staff", "hilda", "2" }, Password)]
    [InlineData(new[] { "create-staff", "hilda", "Manager" }, null)]
    [InlineData(new[] { "create-staff", "hilda", "Manager" }, "")]
    public async Task RunCreateStaffAsync_WrongCommandLine_ReturnsTwoAndCreatesNothing(string[] args, string? password)
    {
        // Arrange
        using var factory = new ApiFactory();

        // Act
        var code = await ApiStartup.RunCreateStaffAsync(factory.Services, args, password, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, code);
        Assert.Empty(factory.World.Staff.Items);
    }

    /// <summary>Negative: a refused account (short password, taken name) returns 1.</summary>
    [Fact]
    public async Task RunCreateStaffAsync_RefusedAccount_ReturnsOne()
    {
        // Arrange
        using var factory = new ApiFactory();
        factory.AddStaff("hilda", Password, StaffRole.Manager);

        // Act
        var taken = await ApiStartup.RunCreateStaffAsync(factory.Services, ["create-staff", "hilda", "Manager"], Password, TestContext.Current.CancellationToken);
        var shortPassword = await ApiStartup.RunCreateStaffAsync(factory.Services, ["create-staff", "other", "Manager"], "short", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, taken);
        Assert.Equal(1, shortPassword);
    }

    #endregion

    #region Helpers

    private sealed class FirstManagerFactory : ApiFactory
    {
        private readonly string _userName;
        private readonly string _password;

        public FirstManagerFactory(string userName, string password)
        {
            _userName = userName;
            _password = password;
        }

        protected override IEnumerable<KeyValuePair<string, string>> ExtraSettings() =>
        [
            new("Staff:InitialManager:UserName", _userName),
            new("Staff:InitialManager:Password", _password),
        ];
    }

    #endregion
}