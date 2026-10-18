using NFHotel.Application.Dto.Staff;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Login;

/// <summary>
/// Presentation logic of the login page (ADR-0005): asks the sign-in use case and keeps the texts of a refusal. The
/// password is checked by the API, never here; this class does not keep it.
/// </summary>
public sealed class LoginViewModel
{
    private readonly IUseCaseRunner<IStaffAuthService> _auth;

    /// <summary>Creates the view model.</summary>
    /// <param name="auth">Runs the sign-in use case.</param>
    public LoginViewModel(IUseCaseRunner<IStaffAuthService> auth)
    {
        ArgumentNullException.ThrowIfNull(auth);

        _auth = auth;
    }

    /// <summary>The refusal, already translated; empty after a success.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    /// <summary>Signs in.</summary>
    /// <param name="userName">The user name as typed.</param>
    /// <param name="password">The password as typed.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The session, or <c>null</c> when the sign-in was refused (see <see cref="Errors"/>).</returns>
    public async Task<StaffSessionDto?> SignInAsync(string? userName, string? password, CancellationToken cancellationToken = default)
    {
        var result = await _auth
            .RunAsync((service, token) => service.SignInAsync(new SignInRequest(userName ?? string.Empty, password ?? string.Empty), token), cancellationToken)
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