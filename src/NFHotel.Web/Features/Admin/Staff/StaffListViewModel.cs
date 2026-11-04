using NFHotel.Application.Dto.Staff;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Staff;

/// <summary>
/// State of the staff page (UC-006, MIL-059): the list of accounts and the forms to create an account, to change its name,
/// e-mail address and role, to disable, enable or delete one and to set a temporary password. The page holds only markup
/// and binding; the API checks every rule again.
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

    /// <summary>The first name in the form of a new account.</summary>
    public string NewFirstName { get; set; } = string.Empty;

    /// <summary>The last name in the form of a new account.</summary>
    public string NewLastName { get; set; } = string.Empty;

    /// <summary>The e-mail address in the form of a new account; it may stay empty.</summary>
    public string NewEmail { get; set; } = string.Empty;

    /// <summary>The account being edited, or <c>null</c>.</summary>
    public StaffAccountDto? EditTarget { get; private set; }

    /// <summary>The first name in the form of an edit.</summary>
    public string EditFirstName { get; set; } = string.Empty;

    /// <summary>The last name in the form of an edit.</summary>
    public string EditLastName { get; set; } = string.Empty;

    /// <summary>The e-mail address in the form of an edit; empty clears it.</summary>
    public string EditEmail { get; set; } = string.Empty;

    /// <summary>The role in the form of an edit.</summary>
    public StaffRole EditRole { get; set; }

    /// <summary>The account that waits for the confirmation of its deletion, or <c>null</c>.</summary>
    public StaffAccountDto? PendingDelete { get; private set; }

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
        Cancel();
        IsCreating = true;
        SuccessMessage = null;
        Errors = Array.Empty<string>();
    }

    /// <summary>Closes the forms and forgets what was typed.</summary>
    public void Cancel()
    {
        IsCreating = false;
        ResetTarget = null;
        PendingDisable = null;
        PendingDelete = null;
        EditTarget = null;
        NewUserName = string.Empty;
        NewPassword = string.Empty;
        NewFirstName = string.Empty;
        NewLastName = string.Empty;
        NewEmail = string.Empty;
        ResetPassword = string.Empty;
        EditFirstName = string.Empty;
        EditLastName = string.Empty;
        EditEmail = string.Empty;
    }

    /// <summary>Opens the form of an edit with the name, the e-mail address and the role of an account.</summary>
    /// <param name="account">The account.</param>
    public void BeginEdit(StaffAccountDto account)
    {
        ArgumentNullException.ThrowIfNull(account);

        Cancel();
        EditTarget = account;
        EditFirstName = account.FirstName ?? string.Empty;
        EditLastName = account.LastName ?? string.Empty;
        EditEmail = account.Email ?? string.Empty;
        EditRole = account.Role;
        SuccessMessage = null;
        Errors = Array.Empty<string>();
    }

    /// <summary>Asks for the confirmation of deleting an account.</summary>
    /// <param name="account">The account.</param>
    public void BeginDelete(StaffAccountDto account)
    {
        ArgumentNullException.ThrowIfNull(account);

        Cancel();
        PendingDelete = account;
        SuccessMessage = null;
        Errors = Array.Empty<string>();
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
        var request = new CreateStaffRequest(
            NewUserName,
            NewPassword,
            NewRole,
            MustChangePassword: true,
            FirstName: Blank(NewFirstName),
            LastName: Blank(NewLastName),
            Email: Blank(NewEmail));
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

    /// <summary>
    /// Saves what changed in the form of an edit: the name, the e-mail address and the role, in that order, each in its
    /// own call. The first refusal stops the rest, and the list shows what was saved before it.
    /// </summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>A task that ends when the list is loaded again.</returns>
    public async Task ConfirmEditAsync(CancellationToken cancellationToken = default)
    {
        if (EditTarget is not { } account)
        {
            return;
        }

        var steps = new List<Func<IStaffAdminService, Task<Result>>>();
        var (first, last, email) = (EditFirstName.Trim(), EditLastName.Trim(), EditEmail.Trim());
        if (first != (account.FirstName ?? string.Empty) || last != (account.LastName ?? string.Empty))
        {
            steps.Add(service => service.RenameAsync(account.Id, new RenameStaffRequest(first, last), cancellationToken));
        }

        if (!string.Equals(email, account.Email ?? string.Empty, StringComparison.OrdinalIgnoreCase))
        {
            steps.Add(service => service.ChangeEmailAsync(account.Id, new ChangeEmailRequest(Blank(email)), cancellationToken));
        }

        var role = EditRole;
        if (role != account.Role)
        {
            steps.Add(service => service.ChangeRoleAsync(account.Id, new ChangeRoleRequest(role), cancellationToken));
        }

        if (steps.Count == 0)
        {
            Cancel();
            return;
        }

        await RunAsync(
            async service =>
            {
                foreach (var step in steps)
                {
                    var result = await step(service).ConfigureAwait(false);
                    if (result.IsFailure)
                    {
                        return result;
                    }
                }

                return Result.Success();
            },
            role != account.Role
                ? $"Saved {account.UserName}. The new role ends the open sessions of the account."
                : $"Saved {account.UserName}.",
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Deletes the account that waits for the confirmation.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>A task that ends when the list is loaded again.</returns>
    public async Task ConfirmDeleteAsync(CancellationToken cancellationToken = default)
    {
        if (PendingDelete is not { } account)
        {
            return;
        }

        await RunAsync(
            service => service.DeleteAsync(account.Id, cancellationToken),
            $"Deleted {account.UserName}. The audit trail keeps the user name.",
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>The name of an account as the list shows it: the first and the last name, or a dash while it has none.</summary>
    /// <param name="account">The account.</param>
    /// <returns>The name.</returns>
    public static string NameOf(StaffAccountDto account)
    {
        ArgumentNullException.ThrowIfNull(account);

        return account.FirstName is { } first && account.LastName is { } last ? $"{first} {last}" : "—";
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

    private static string? Blank(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
