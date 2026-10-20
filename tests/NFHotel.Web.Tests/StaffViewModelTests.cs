using System.Security.Claims;

using NFHotel.Application.Dto.Staff;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;
using NFHotel.Web.Features.Admin.Staff;
using NFHotel.Web.Security;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>The presentation logic of the change-password, staff and audit pages, and the must-change claim (UC-005, UC-006, ADR-0017).</summary>
public sealed class StaffViewModelTests
{
    private static readonly DateTimeOffset Later = new(2100, 1, 1, 0, 0, 0, TimeSpan.Zero);

    #region The claim

    /// <summary>Equivalence: the cookie says whether the member must change the password; nobody signed in never must.</summary>
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void MustChangePassword_SignedInUser_FollowsTheSession(bool mustChange, bool expected)
    {
        // Arrange
        var principal = StaffClaims.CreatePrincipal(new StaffSessionDto("t", Later, "x", StaffRole.Manager, mustChange));

        // Act and assert
        Assert.Equal(expected, StaffClaims.MustChangePassword(principal));
        Assert.False(StaffClaims.MustChangePassword(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    #endregion

    #region Change password

    /// <summary>Positive and negative: a success returns the session; a refusal returns nothing and keeps the translated text.</summary>
    [Fact]
    public async Task ChangeAsync_SuccessThenRefusal_ReturnsTheSessionThenTheTranslatedText()
    {
        // Arrange
        var session = new StaffSessionDto("t", Later, "rita", StaffRole.Receptionist);
        var auth = Substitute.For<IStaffAuthService>();
        auth.ChangePasswordAsync(Arg.Any<ChangePasswordRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<StaffSessionDto>.Success(session), Result<StaffSessionDto>.Failure(ErrorCodes.Staff.PasswordTooCommon));
        var viewModel = new ChangePasswordViewModel(Runner(auth));

        // Act
        var first = await viewModel.ChangeAsync("old", "new", TestContext.Current.CancellationToken);
        var second = await viewModel.ChangeAsync("old", "new", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(session, first);
        Assert.Null(second);
        Assert.Equal(["That password is too common. Choose another one."], viewModel.Errors);
    }

    /// <summary>Edge: a missing password is sent as empty text.</summary>
    [Fact]
    public async Task ChangeAsync_MissingPasswords_SendsEmptyText()
    {
        // Arrange
        var auth = Substitute.For<IStaffAuthService>();
        auth.ChangePasswordAsync(Arg.Any<ChangePasswordRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<StaffSessionDto>.Failure(ErrorCodes.Staff.CurrentPasswordInvalid));

        // Act
        await new ChangePasswordViewModel(Runner(auth)).ChangeAsync(null, null, TestContext.Current.CancellationToken);

        // Assert
        await auth.Received(1).ChangePasswordAsync(new ChangePasswordRequest(string.Empty, string.Empty), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Staff page

    /// <summary>Positive: loading fills the list; creating sends the typed fields with the must-change flag, closes the form and loads again.</summary>
    [Fact]
    public async Task CreateAsync_Success_SendsTheFieldsClosesTheFormAndReloads()
    {
        // Arrange
        var admin = Substitute.For<IStaffAdminService>();
        admin.ListAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<StaffAccountDto>>.Success([]));
        admin.CreateAsync(Arg.Any<CreateStaffRequest>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        var viewModel = new StaffListViewModel(Runner(admin));
        viewModel.BeginCreate();
        viewModel.NewUserName = " Lena ";
        viewModel.NewPassword = "a temporary phrase";
        viewModel.NewRole = StaffRole.Housekeeping;

        // Act
        await viewModel.CreateAsync(TestContext.Current.CancellationToken);

        // Assert
        await admin.Received(1).CreateAsync(new CreateStaffRequest(" Lena ", "a temporary phrase", StaffRole.Housekeeping, true), Arg.Any<CancellationToken>());
        await admin.Received(1).ListAsync(Arg.Any<CancellationToken>());
        Assert.False(viewModel.IsCreating);
        Assert.Equal(string.Empty, viewModel.NewPassword);
        Assert.Contains("Lena", viewModel.SuccessMessage, StringComparison.Ordinal);
        Assert.Empty(viewModel.Errors);
    }

    /// <summary>Negative: a refusal keeps the form open with the typed values and shows the translated text.</summary>
    [Fact]
    public async Task ConfirmDisableAsync_LastManager_KeepsTheConfirmationAndShowsTheText()
    {
        // Arrange
        var admin = Substitute.For<IStaffAdminService>();
        admin.DisableAsync(1, Arg.Any<CancellationToken>()).Returns(Result.Failure(ErrorCodes.Staff.LastManager));
        var viewModel = new StaffListViewModel(Runner(admin));
        var boss = new StaffAccountDto(1, "boss", StaffRole.Manager, true, false);
        viewModel.BeginDisable(boss);

        // Act
        await viewModel.ConfirmDisableAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["At least one active Manager must remain, so this account cannot be disabled."], viewModel.Errors);
        Assert.Equal(boss, viewModel.PendingDisable);
        Assert.Null(viewModel.SuccessMessage);
    }

    /// <summary>State transition: enabling and resetting each call their use case, and a reset forgets the typed password.</summary>
    [Fact]
    public async Task EnableAndReset_Success_CallTheirUseCases()
    {
        // Arrange
        var admin = Substitute.For<IStaffAdminService>();
        admin.ListAsync(Arg.Any<CancellationToken>()).Returns(Result<IReadOnlyList<StaffAccountDto>>.Success([]));
        admin.EnableAsync(2, Arg.Any<CancellationToken>()).Returns(Result.Success());
        admin.ResetPasswordAsync(2, Arg.Any<ResetPasswordRequest>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        var viewModel = new StaffListViewModel(Runner(admin));
        var rita = new StaffAccountDto(2, "rita", StaffRole.Receptionist, false, false);

        // Act
        await viewModel.EnableAsync(rita, TestContext.Current.CancellationToken);
        viewModel.BeginReset(rita);
        viewModel.ResetPassword = "a temporary phrase";
        await viewModel.ConfirmResetAsync(TestContext.Current.CancellationToken);

        // Assert
        await admin.Received(1).EnableAsync(2, Arg.Any<CancellationToken>());
        await admin.Received(1).ResetPasswordAsync(2, new ResetPasswordRequest("a temporary phrase"), Arg.Any<CancellationToken>());
        Assert.Null(viewModel.ResetTarget);
        Assert.Equal(string.Empty, viewModel.ResetPassword);
    }

    /// <summary>Edge: confirming with nothing pending calls nothing.</summary>
    [Fact]
    public async Task ConfirmDisableAndReset_NothingPending_CallNothing()
    {
        // Arrange
        var admin = Substitute.For<IStaffAdminService>();
        var viewModel = new StaffListViewModel(Runner(admin));

        // Act
        await viewModel.ConfirmDisableAsync(TestContext.Current.CancellationToken);
        await viewModel.ConfirmResetAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(admin.ReceivedCalls());
    }

    #endregion

    #region Audit page

    /// <summary>Boundary: the first page has no previous page, the last has no next, and a refusal keeps its text.</summary>
    [Fact]
    public async Task LoadAsync_Pages_SetThePreviousAndNextFlags()
    {
        // Arrange
        var admin = Substitute.For<IStaffAdminService>();
        admin.ListAuditAsync(1, StaffAuditViewModel.PageSize, Arg.Any<CancellationToken>())
            .Returns(Result<StaffAuditPageDto>.Success(new StaffAuditPageDto([], 1, StaffAuditViewModel.PageSize, 120)));
        admin.ListAuditAsync(3, StaffAuditViewModel.PageSize, Arg.Any<CancellationToken>())
            .Returns(Result<StaffAuditPageDto>.Success(new StaffAuditPageDto([], 3, StaffAuditViewModel.PageSize, 120)));
        admin.ListAuditAsync(4, StaffAuditViewModel.PageSize, Arg.Any<CancellationToken>())
            .Returns(Result<StaffAuditPageDto>.Failure(ErrorCodes.Api.Forbidden));
        var viewModel = new StaffAuditViewModel(Runner(admin));

        // Act and assert
        await viewModel.LoadAsync(1, TestContext.Current.CancellationToken);
        Assert.False(viewModel.HasPrevious);
        Assert.True(viewModel.HasNext);
        await viewModel.LoadAsync(3, TestContext.Current.CancellationToken);
        Assert.True(viewModel.HasPrevious);
        Assert.False(viewModel.HasNext);
        await viewModel.LoadAsync(4, TestContext.Current.CancellationToken);
        Assert.Equal(["Your role isn't allowed to do that."], viewModel.Errors);
    }

    #endregion

    #region Helpers

    private static IUseCaseRunner<TService> Runner<TService>(TService service)
        where TService : class
    {
        var runner = Substitute.For<IUseCaseRunner<TService>>();
        runner.RunAsync(Arg.Any<Func<TService, CancellationToken, Task<Result>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<TService, CancellationToken, Task<Result>>>()(service, call.Arg<CancellationToken>()));
        runner.RunAsync(Arg.Any<Func<TService, CancellationToken, Task<Result<StaffSessionDto>>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<TService, CancellationToken, Task<Result<StaffSessionDto>>>>()(service, call.Arg<CancellationToken>()));
        runner.RunAsync(Arg.Any<Func<TService, CancellationToken, Task<Result<IReadOnlyList<StaffAccountDto>>>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<TService, CancellationToken, Task<Result<IReadOnlyList<StaffAccountDto>>>>>()(service, call.Arg<CancellationToken>()));
        runner.RunAsync(Arg.Any<Func<TService, CancellationToken, Task<Result<StaffAuditPageDto>>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<TService, CancellationToken, Task<Result<StaffAuditPageDto>>>>()(service, call.Arg<CancellationToken>()));
        return runner;
    }

    #endregion
}
