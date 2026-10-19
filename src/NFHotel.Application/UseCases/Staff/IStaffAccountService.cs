using NFHotel.Application.Dto.Staff;

namespace NFHotel.Application.UseCases.Staff;

/// <summary>Creating staff accounts. It has no endpoint: it is used when the API starts and by its command line (ADR-0005).</summary>
public interface IStaffAccountService
{
    /// <summary>Creates an active account.</summary>
    /// <param name="request">The user name, the password and the role.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Success, or the codes of the fields that are wrong, or <c>staff.user_name_taken</c>.</returns>
    Task<Result> CreateAsync(CreateStaffRequest request, CancellationToken cancellationToken = default);

    /// <summary>Creates the first Manager when no account exists. Does nothing when one does.</summary>
    /// <param name="userName">The user name.</param>
    /// <param name="password">The password.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Success whether it created the account or found one; the codes if the account was refused.</returns>
    Task<Result> EnsureFirstManagerAsync(string userName, string password, CancellationToken cancellationToken = default);
}
