using NFHotel.Application.Dto.Staff;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Staff;

/// <summary>
/// State of the staff page (UC-006): the list of accounts and the forms to create an account, to disable or enable one
/// and to set a temporary password. The page holds only markup and binding; the API checks every rule again.
/// </summary>
public sealed class StaffListViewModel
{
    private readonly IUseCaseRunner<IStaffAdminService> _admin;

    /// <summary>Creates the view model.</summary>
    /// <param name="admin">Runs the use cases of a Manager.</param>
    public StaffListViewModel(IUseCaseRunner<IStaffAdminService> admin)
    {
        ArgumentNullException.ThrowIfNull(admin);

        _admin = admin;
    }

    /// <summary>The roles that can be given to a new account.</summary>
    public static IReadOnlyList<StaffRole> Roles { get; } = Enum.GetValues<StaffRole>();

    /// <summary>The accounts shown right now.</summary>
    public IReadOnlyList<StaffAccountDto> Accounts { get; private set; } = Array.Empty<StaffAccountDto>();

    /// <summary>The texts of the last refusal, empty when the last call succeeded.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    /// <summary>The receipt of the last success, or <c>null</c>.</summary>
    public string? SuccessMessage { get; private set; }

    /// <summary>True while a call is running.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>True once the accounts have been asked for.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>True while the form of a new account is open.</summary>
    public bool IsCreating { get; private set; }

    /// <summary>The user name in the form of a new account.</summary>
    public string NewUserName { get; set; } = string.Empty;

    /// <summary>The temporary password in the form of a new account.</summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>The role in the form of a new account.</summary>
    public StaffRole NewRole { get; set; } = StaffRole.Receptionist;

    /// <summary>The account whose password is being reset, or <c>null</c>.</summary>
    public StaffAccountDto? ResetTarget { get; private set; }

    /// <summary>The temporary password in the form of a reset.</summary>
    public string ResetPassword { get; set; } = string.Empty;

    /// <summary>The account that waits for the confirmation of its disabling, or <c>null</c>.</summary>
    public StaffAccountDto? PendingDisable { get; private set; }

    /// <summary>Loads the accounts.</summary>
    /// <param name="cancellationToken">Cancels the call when the page is closed.</param>
    /// <returns>A task that ends when the list is loaded.</returns>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        try
        {
            var result = await _admin.RunAsync((service, token) => service.ListAsync(token), cancellationToken).ConfigureAwait(false);
            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                Accounts = Array.Empty<StaffAccountDto>();
            }
            else
            {
                Errors = Array.Empty<string>();
                Accounts = result.Value;
            }
        }
        finally
        {
            IsBusy = false;
            IsLoaded = true;
        }
    }

    /// <summary>Opens the form of a new account.</summary>
    public void BeginCreate()
    {
        IsCreating = true;
        ResetTarget = null;
        PendingDisable = null;
        SuccessMessage = null;
        Errors = Array.Empty<string>();
    }

    /// <summary>Closes the forms and forgets what was typed.</summary>
    public void Cancel()
    {
        IsCreating = false;
        ResetTarget = null;
        PendingDisable = null;
        NewUserName = string.Empty;
        NewPassword = string.Empty;
        ResetPassword = string.Empty;
    }

    /// <summary>Opens the form of a reset for an account.</summary>
    /// <param name="account">The account.</param>
    public void BeginReset(StaffAccountDto account)
    {
        ArgumentNullException.ThrowIfNull(account);

        Cancel();
        ResetTarget = account;
        SuccessMessage = null;
        Errors = Array.Empty<string>();
    }

    /// <summary>Asks for the confirmation of disabling an account.</summary>
    /// <param name="account">The account.</param>
    public void BeginDisable(StaffAccountDto account)
    {
        ArgumentNullException.ThrowIfNull(account);

        Cancel();
        PendingDisable = account;
        SuccessMessage = null;
        Errors = Array.Empty<string>();
    }

    /// <summary>Creates the account of the form.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>A task that ends when the list is loaded again.</returns>
    public async Task CreateAsync(CancellationToken cancellationToken = default)
    {
        var request = new CreateStaffRequest(NewUserName, NewPassword, NewRole, MustChangePassword: true);
        var name = NewUserName.Trim();
        await RunAsync(
            service => service.CreateAsync(request, cancellationToken),
            $"Created {name}. The person must choose a new password at the first sign-in.",
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Disables the account that waits for the confirmation.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>A task that ends when the list is loaded again.</returns>
    public async Task ConfirmDisableAsync(CancellationToken cancellationToken = default)
    {
        if (PendingDisable is not { } account)
        {
            return;
        }

        await RunAsync(service => service.DisableAsync(account.Id, cancellationToken), $"Disabled {account.UserName}. Their sessions have ended.", cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Enables an account again.</summary>
    /// <param name="account">The account.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>A task that ends when the list is loaded again.</returns>
    public async Task EnableAsync(StaffAccountDto account, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);

        await RunAsync(
            service => service.EnableAsync(account.Id, cancellationToken),
            $"Enabled {account.UserName}. The person must choose a new password at the next sign-in.",
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sets the temporary password of the form for the account being reset.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>A task that ends when the list is loaded again.</returns>
    public async Task ConfirmResetAsync(CancellationToken cancellationToken = default)
    {
        if (ResetTarget is not { } account)
        {
            return;
        }

        var request = new ResetPasswordRequest(ResetPassword);
        await RunAsync(
            service => service.ResetPasswordAsync(account.Id, request, cancellationToken),
            $"Set a temporary password for {account.UserName}. The person must change it at the next sign-in.",
            cancellationToken).ConfigureAwait(false);
    }

    private async Task RunAsync(Func<IStaffAdminService, Task<Result>> action, string success, CancellationToken cancellationToken)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _admin.RunAsync((service, _) => action(service), cancellationToken).ConfigureAwait(false);
            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                SuccessMessage = null;
                return;
            }

            Errors = Array.Empty<string>();
            SuccessMessage = success;
            Cancel();
        }
        finally
        {
            IsBusy = false;
        }

        await LoadAsync(cancellationToken).ConfigureAwait(false);
    }
}
