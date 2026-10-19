using NFHotel.Application.Dto.Staff;

namespace NFHotel.Application.UseCases.Staff;

/// <summary>The sign-in of the back office (ADR-0005).</summary>
public interface IStaffAuthService
{
    /// <summary>
    /// Signs a member of staff in. An unknown user, a wrong password and an inactive account give the same error.
    /// </summary>
    /// <param name="request">The user name and password.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>
    /// The session, or <c>staff.invalid_credentials</c>, or <c>staff.too_many_attempts</c> when the user name
    /// has used up its attempts.
    /// </returns>
    Task<Result<StaffSessionDto>> SignInAsync(SignInRequest request, CancellationToken cancellationToken = default);
}
