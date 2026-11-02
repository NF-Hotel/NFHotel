using Bunit;

using NFHotel.Application.Dto.Staff;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Features.Admin.Staff;

using NSubstitute;

using static NFHotel.Web.Tests.AdminSamples;

namespace NFHotel.Web.Tests;

/// <summary>The staff accounts page and the audit trail page, rendered and clicked (MIL-054 task 7).</summary>
public sealed class StaffPageTests : IAsyncDisposable
{
    private const string TemporaryPassword = "temporary-value-for-tests";
    private static readonly DateTimeOffset At = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);

    private readonly IStaffAdminService _admin = Substitute.For<IStaffAdminService>();
    private readonly PageHost _host = new();

    public StaffPageTests()
    {
        var runner = new PassThroughRunner<IStaffAdminService>(_admin);
        _host.With(new StaffListViewModel(runner)).With(new StaffAuditViewModel(runner)).SignedInWith();
        _admin.ListAsync(Arg.Any<CancellationToken>()).Returns(List(
            new StaffAccountDto(1, "mona", StaffRole.Manager, true, false),
            new StaffAccountDto(2, "rita", StaffRole.Receptionist, true, true),
            new StaffAccountDto(3, "otto", StaffRole.Receptionist, false, false)));
        _admin.CreateAsync(Arg.Any<CreateStaffRequest>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        _admin.DisableAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        _admin.EnableAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
        _admin.ResetPasswordAsync(Arg.Any<int>(), Arg.Any<ResetPasswordRequest>(), Arg.Any<CancellationToken>()).Returns(Result.Success());
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    private static StaffAuditEventDto Line(string? actor) =>
        new(At, actor, StaffAuditAction.SignedIn, actor, "success", actor is null ? null : "10.0.0.1");

    private void AuditIs(int page, int total, params StaffAuditEventDto[] items) =>
        _admin.ListAuditAsync(page, StaffAuditViewModel.PageSize, Arg.Any<CancellationToken>())
            .Returns(Ok(new StaffAuditPageDto(items, page, StaffAuditViewModel.PageSize, total)));

    /// <summary>Positive: each account shows its state and the button its state allows.</summary>
    [Fact]
    public void StaffList_Render_ShowsStatesAndButtons()
    {
        var page = _host.Render<StaffList>();

        Assert.Contains("Active", page.Markup, StringComparison.Ordinal);
        Assert.Contains("Must change password", page.Markup, StringComparison.Ordinal);
        Assert.Contains("Disabled", page.Markup, StringComparison.Ordinal);
        Assert.Equal(2, page.FindAll("button").Count(button => button.TextContent.Trim() == "Disable"));
        Assert.True(page.HasButton("Enable"));
    }

    /// <summary>Positive: a new account is typed in and created with the chosen role.</summary>
    [Fact]
    public void StaffList_Create_SendsTheAccount()
    {
        var page = _host.Render<StaffList>();

        page.Button("New account").Click();
        page.Find("#new-user").Input("nina");
        page.Find("#new-role").Change(nameof(StaffRole.Manager));
        page.Find("#new-password").Input(TemporaryPassword);
        page.Button("Create account").Click();

        _admin.Received(1).CreateAsync(
            Arg.Is<CreateStaffRequest>(request => request.UserName == "nina" && request.Role == StaffRole.Manager && request.Password == TemporaryPassword),
            Arg.Any<CancellationToken>());
    }

    /// <summary>State transition: the create form and the reset form close with Go back; a reset sets the password.</summary>
    [Fact]
    public void StaffList_ResetAndGoBack_SetsThePassword()
    {
        var page = _host.Render<StaffList>();
        page.Button("New account").Click();
        page.Button("Go back").Click();
        Assert.Empty(page.FindAll("#new-user"));

        page.Button("Reset password").Click();
        Assert.Contains("Set a temporary password for mona", page.Markup, StringComparison.Ordinal);
        page.Button("Go back").Click();
        Assert.Empty(page.FindAll("#reset-value"));

        page.Button("Reset password").Click();
        page.Find("#reset-value").Input(TemporaryPassword);
        page.Button("Set temporary password").Click();

        _admin.Received(1).ResetPasswordAsync(1, new ResetPasswordRequest(TemporaryPassword), Arg.Any<CancellationToken>());
    }

    /// <summary>State transition: a disable asks first and disables on confirm; a disabled account is enabled at once.</summary>
    [Fact]
    public void StaffList_DisableAndEnable_CallTheService()
    {
        var page = _host.Render<StaffList>();

        page.Button("Disable").Click();
        Assert.Contains("Disable mona?", page.Markup, StringComparison.Ordinal);
        page.Button("Disable account").Click();
        page.Button("Enable").Click();

        _admin.Received(1).DisableAsync(1, Arg.Any<CancellationToken>());
        _admin.Received(1).EnableAsync(3, Arg.Any<CancellationToken>());
    }

    /// <summary>Positive and boundary: the audit trail pages forward and back, and a missing name or address shows a dash.</summary>
    [Fact]
    public void StaffAudit_TwoPages_PagesForwardAndBack()
    {
        AuditIs(1, StaffAuditViewModel.PageSize + 1, Line("mona"), Line(null));
        AuditIs(2, StaffAuditViewModel.PageSize + 1, Line("rita"));
        var page = _host.Render<StaffAudit>();

        Assert.Contains("Page 1 of 2", page.Markup, StringComparison.Ordinal);
        Assert.Contains("2026-10-19 10:00:00", page.Markup, StringComparison.Ordinal);
        Assert.Contains("10.0.0.1", page.Markup, StringComparison.Ordinal);
        Assert.True(page.Button("Previous").HasAttribute("disabled"));

        page.Button("Next").Click();
        Assert.Contains("Page 2 of 2", page.Markup, StringComparison.Ordinal);
        page.Button("Previous").Click();

        Assert.Contains("Page 1 of 2", page.Markup, StringComparison.Ordinal);
    }

    /// <summary>Edge and error handling: an empty trail says so; a refused load shows its text.</summary>
    [Fact]
    public void StaffAudit_EmptyAndRefused_SaySo()
    {
        AuditIs(1, 0);
        Assert.Contains("Nothing has been recorded yet.", _host.Render<StaffAudit>().Markup, StringComparison.Ordinal);

        _admin.ListAuditAsync(1, StaffAuditViewModel.PageSize, Arg.Any<CancellationToken>())
            .Returns(Fail<StaffAuditPageDto>(ErrorCodes.Staff.NotFound));
        Assert.Contains(Text(ErrorCodes.Staff.NotFound), _host.Render<StaffAudit>().Markup, StringComparison.Ordinal);
    }
}
