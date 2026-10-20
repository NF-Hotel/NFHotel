using NFHotel.Application.Dto.Staff;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Staff;

/// <summary>
/// Presentation logic of the change-password page (UC-005): asks the use case and keeps the texts of a refusal. The
/// passwords are checked by the API, never here, and this class does not keep them.
/// </summary>
public sealed class ChangePasswordViewModel
{
    private readonly IUseCaseRunner<IStaffAuthService> _auth;

    /// <summary>Creates the view model.</summary>
    /// <param name="auth">Runs the use case.</param>
    public ChangePasswordViewModel(IUseCaseRunner<IStaffAuthService> auth)
    {
        ArgumentNullException.ThrowIfNull(auth);

        _auth = auth;
    }

    /// <summary>The refusal, already translated; empty after a success.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    /// <summary>Changes the password of the signed-in member.</summary>
    /// <param name="currentPassword">The current password as typed.</param>
    /// <param name="newPassword">The new password as typed.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The new session, or <c>null</c> when the change was refused (see <see cref="Errors"/>).</returns>
    public async Task<StaffSessionDto?> ChangeAsync(string? currentPassword, string? newPassword, CancellationToken cancellationToken = default)
    {
        var result = await _auth
            .RunAsync(
                (service, token) => service.ChangePasswordAsync(new ChangePasswordRequest(currentPassword ?? string.Empty, newPassword ?? string.Empty), token),
                cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            Errors = ErrorMessages.Describe(result);
            return null;
        }

        Errors = Array.Empty<string>();
        return result.Value;
    }
}
