using NFHotel.Application.Dto.Staff;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;

namespace NFHotel.Application.UseCases.Staff;

/// <summary>Checks a password and issues the token (ADR-0005).</summary>
public sealed class StaffAuthService : IStaffAuthService
{
    /// <summary>A longer password is refused without hashing it, so it cannot be used to burn processor time.</summary>
    public const int MaxPasswordLength = 256;

    private readonly IStaffRepository _staff;
    private readonly IPasswordHasher _hasher;
    private readonly IStaffTokenIssuer _tokens;
    private readonly ILoginThrottle _throttle;

    /// <summary>Creates the service.</summary>
    /// <param name="staff">The staff accounts.</param>
    /// <param name="hasher">Checks the password.</param>
    /// <param name="tokens">Issues the token.</param>
    /// <param name="throttle">Counts failed attempts.</param>
    public StaffAuthService(
        IStaffRepository staff,
        IPasswordHasher hasher,
        IStaffTokenIssuer tokens,
        ILoginThrottle throttle)
    {
        ArgumentNullException.ThrowIfNull(staff);
        ArgumentNullException.ThrowIfNull(hasher);
        ArgumentNullException.ThrowIfNull(tokens);
        ArgumentNullException.ThrowIfNull(throttle);

        _staff = staff;
        _hasher = hasher;
        _tokens = tokens;
        _throttle = throttle;
    }

    /// <inheritdoc />
    public async Task<Result<StaffSessionDto>> SignInAsync(
        SignInRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var key = StaffMember.NormalizeUserName(request.UserName);
        if (_throttle.IsBlocked(key))
        {
            return Result<StaffSessionDto>.Failure(ErrorCodes.Staff.TooManyAttempts);
        }

        var password = request.Password ?? string.Empty;
        var member = key.Length == 0 || password.Length > MaxPasswordLength
            ? null
            : await _staff.FindByUserNameAsync(key, cancellationToken).ConfigureAwait(false);

        // The hash is checked for a missing member too (against a dummy), so the time does not tell the cases apart.
        var passwordMatches = _hasher.Verify(member?.PasswordHash, password);
        if (member is null || !member.IsActive || !passwordMatches)
        {
            _throttle.RecordFailure(key);
            return Result<StaffSessionDto>.Failure(ErrorCodes.Staff.InvalidCredentials);
        }

        _throttle.Reset(key);
        var issued = _tokens.Issue(member);
        return Result<StaffSessionDto>.Success(new StaffSessionDto(issued.Token, issued.ExpiresAt, member.UserName, member.Role));
    }
}
