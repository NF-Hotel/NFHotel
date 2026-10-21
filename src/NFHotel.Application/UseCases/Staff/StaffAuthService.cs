using NFHotel.Application.Dto.Staff;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Staff;

/// <summary>Checks a password, issues the token and changes the own password (ADR-0005, ADR-0017).</summary>
public sealed class StaffAuthService : IStaffAuthService
{
    /// <summary>A longer password is refused without hashing it, so it cannot be used to burn processor time.</summary>
    public const int MaxPasswordLength = PasswordPolicy.MaxLength;

    private readonly IStaffRepository _staff;
    private readonly IPasswordHasher _hasher;
    private readonly IStaffTokenIssuer _tokens;
    private readonly ILoginThrottle _throttle;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStaffCallContext _context;
    private readonly StaffAuditRecorder _recorder;

    /// <summary>Creates the service.</summary>
    /// <param name="staff">The staff accounts.</param>
    /// <param name="hasher">Checks the password.</param>
    /// <param name="tokens">Issues the token.</param>
    /// <param name="throttle">Counts failed attempts.</param>
    /// <param name="unitOfWork">The transaction boundary.</param>
    /// <param name="audit">The audit trail.</param>
    /// <param name="context">Who is calling.</param>
    /// <param name="clock">The source of the time.</param>
    public StaffAuthService(
        IStaffRepository staff,
        IPasswordHasher hasher,
        IStaffTokenIssuer tokens,
        ILoginThrottle throttle,
        IUnitOfWork unitOfWork,
        IStaffAuditRepository audit,
        IStaffCallContext context,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(staff);
        ArgumentNullException.ThrowIfNull(hasher);
        ArgumentNullException.ThrowIfNull(tokens);
        ArgumentNullException.ThrowIfNull(throttle);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(clock);

        _staff = staff;
        _hasher = hasher;
        _tokens = tokens;
        _throttle = throttle;
        _unitOfWork = unitOfWork;
        _context = context;
        _recorder = new StaffAuditRecorder(audit, context, clock);
    }

    /// <summary>The key of the limit on the current-password check of one account (ADR-0017).</summary>
    /// <param name="staffId">The account.</param>
    /// <returns>The key.</returns>
    public static string ChangeKey(int staffId) => $"pwd:{staffId}";

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
            await RecordSignInFailureAsync(member, key, cancellationToken).ConfigureAwait(false);
            return Result<StaffSessionDto>.Failure(ErrorCodes.Staff.InvalidCredentials);
        }

        _throttle.Reset(key);
        _recorder.BySelf(StaffAuditAction.SignedIn, StaffAuditRecorder.Success, member);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<StaffSessionDto>.Success(SessionOf(member));
    }

    /// <inheritdoc />
    public async Task<Result<StaffSessionDto>> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_context.StaffId is not { } id
            || await _staff.GetByIdAsync(id, cancellationToken).ConfigureAwait(false) is not { IsActive: true } member)
        {
            return Result<StaffSessionDto>.Failure(ErrorCodes.Api.StaffUnauthorized);
        }

        var key = ChangeKey(member.Id);
        if (_throttle.IsBlocked(key))
        {
            return Result<StaffSessionDto>.Failure(ErrorCodes.Staff.TooManyAttempts);
        }

        var current = request.CurrentPassword ?? string.Empty;
        var matches = current.Length <= MaxPasswordLength && _hasher.Verify(member.PasswordHash, current);
        if (!matches)
        {
            _throttle.RecordFailure(key);
            return await RefuseChangeAsync(member, ErrorCodes.Staff.CurrentPasswordInvalid, cancellationToken).ConfigureAwait(false);
        }

        var code = PasswordPolicy.Check(request.NewPassword, member.UserName, current);
        if (code is not null)
        {
            return await RefuseChangeAsync(member, code, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            member.ChangePassword(_hasher.Hash(request.NewPassword));
        }
        catch (DomainException exception)
        {
            return Result<StaffSessionDto>.FromCodes(exception.Codes);
        }

        _staff.Update(member);
        _throttle.Reset(key);
        _recorder.BySelf(StaffAuditAction.PasswordChanged, StaffAuditRecorder.Success, member);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<StaffSessionDto>.Success(SessionOf(member));
    }

    private StaffSessionDto SessionOf(StaffMember member)
    {
        var issued = _tokens.Issue(member);
        return new StaffSessionDto(issued.Token, issued.ExpiresAt, member.UserName, member.Role, member.MustChangePassword);
    }

    private async Task<Result<StaffSessionDto>> RefuseChangeAsync(StaffMember member, string code, CancellationToken cancellationToken)
    {
        _recorder.BySelf(StaffAuditAction.PasswordChangeFailed, code, member);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<StaffSessionDto>.Failure(code);
    }

    // Only an existing account is written to the trail, so guessing at made-up names cannot fill the table.
    private async Task RecordSignInFailureAsync(StaffMember? member, string key, CancellationToken cancellationToken)
    {
        if (member is null)
        {
            return;
        }

        _recorder.BySelf(StaffAuditAction.SignInFailed, ErrorCodes.Staff.InvalidCredentials, member);
        if (_throttle.IsBlocked(key))
        {
            _recorder.BySelf(StaffAuditAction.LockedOut, ErrorCodes.Staff.TooManyAttempts, member);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
