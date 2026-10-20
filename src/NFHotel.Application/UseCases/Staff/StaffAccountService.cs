using NFHotel.Application.Dto.Staff;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Staff;

/// <summary>Creates and manages staff accounts and reads the audit trail (ADR-0005, ADR-0017).</summary>
public sealed class StaffAccountService : IStaffAccountService, IStaffAdminService
{
    /// <summary>The shortest password that is accepted.</summary>
    public const int MinPasswordLength = PasswordPolicy.MinLength;

    /// <summary>The most lines that one page of the audit trail holds.</summary>
    public const int MaxAuditPageSize = 200;

    private readonly IStaffRepository _staff;
    private readonly IPasswordHasher _hasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoginThrottle _throttle;
    private readonly IStaffAuditRepository _audit;
    private readonly IStaffCallContext _context;
    private readonly StaffAuditRecorder _recorder;

    /// <summary>Creates the service.</summary>
    /// <param name="staff">The staff accounts.</param>
    /// <param name="hasher">Hashes the password.</param>
    /// <param name="unitOfWork">The transaction boundary.</param>
    /// <param name="throttle">The sign-in limits, cleared by a reset or an enable.</param>
    /// <param name="audit">The audit trail.</param>
    /// <param name="context">Who is calling.</param>
    /// <param name="clock">The source of the time.</param>
    public StaffAccountService(
        IStaffRepository staff,
        IPasswordHasher hasher,
        IUnitOfWork unitOfWork,
        ILoginThrottle throttle,
        IStaffAuditRepository audit,
        IStaffCallContext context,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(staff);
        ArgumentNullException.ThrowIfNull(hasher);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(throttle);
        ArgumentNullException.ThrowIfNull(audit);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(clock);

        _staff = staff;
        _hasher = hasher;
        _unitOfWork = unitOfWork;
        _throttle = throttle;
        _audit = audit;
        _context = context;
        _recorder = new StaffAuditRecorder(audit, context, clock);
    }

    /// <inheritdoc />
    public async Task<Result> CreateAsync(CreateStaffRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var password = request.Password ?? string.Empty;
        var passwordCode = PasswordPolicy.Check(password, request.UserName);
        if (passwordCode is not null)
        {
            return Result.Failure(passwordCode);
        }

        StaffMember member;
        try
        {
            member = StaffMember.Create(request.UserName, _hasher.Hash(password), request.Role, request.MustChangePassword);
        }
        catch (DomainException exception)
        {
            return Result.FromCodes(exception.Codes);
        }

        if (await _staff.FindByUserNameAsync(member.UserName, cancellationToken).ConfigureAwait(false) is not null)
        {
            return Result.Failure(ErrorCodes.Staff.UserNameTaken);
        }

        // The account has no key before it is saved, so the record names it by user name only. This keeps the account and
        // its record in one transaction.
        _staff.Add(member);
        _recorder.ByCaller(StaffAuditAction.AccountCreated, StaffAuditRecorder.Success, member, withTargetId: false);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (UniqueConstraintViolationException)
        {
            // Two starts at the same moment: the other one created the account first.
            return Result.Failure(ErrorCodes.Staff.UserNameTaken);
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public Task<Result> EnsureFirstManagerAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default) =>
        EnsureFirstManagerAsync(userName, password, true, cancellationToken);

    /// <inheritdoc />
    public async Task<Result> EnsureFirstManagerAsync(
        string userName,
        string password,
        bool mustChangePassword,
        CancellationToken cancellationToken = default)
    {
        if (await _staff.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return Result.Success();
        }

        var created = await CreateAsync(
                new CreateStaffRequest(userName, password, StaffRole.Manager, mustChangePassword),
                cancellationToken)
            .ConfigureAwait(false);

        // A taken name here means another start won the race: the account exists, which is what was asked.
        return created.IsFailure && created.FirstError?.Code == ErrorCodes.Staff.UserNameTaken
            ? Result.Success()
            : created;
    }

    /// <inheritdoc />
    public async Task<Result> ResetPasswordByNameAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        var member = await _staff.FindByUserNameAsync(userName ?? string.Empty, cancellationToken).ConfigureAwait(false);
        return member is null
            ? Result.Failure(ErrorCodes.Staff.NotFound)
            : await ApplyResetAsync(member, password, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<StaffAccountDto>>> ListAsync(CancellationToken cancellationToken = default)
    {
        var members = await _staff.ListAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<StaffAccountDto> items =
            [.. members.Select(member => new StaffAccountDto(member.Id, member.UserName, member.Role, member.IsActive, member.MustChangePassword))];
        return Result<IReadOnlyList<StaffAccountDto>>.Success(items);
    }

    /// <inheritdoc />
    public async Task<Result> DisableAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id == _context.StaffId)
        {
            return Result.Failure(ErrorCodes.Staff.CannotDisableSelf);
        }

        var member = await _staff.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (member is null)
        {
            return Result.Failure(ErrorCodes.Staff.NotFound);
        }

        if (member.IsActive
            && member.Role == StaffRole.Manager
            && !await _staff.AnotherActiveManagerExistsAsync(member.Id, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure(ErrorCodes.Staff.LastManager);
        }

        member.Deactivate();
        _staff.Update(member);
        _recorder.ByCaller(StaffAuditAction.AccountDisabled, StaffAuditRecorder.Success, member);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> EnableAsync(int id, CancellationToken cancellationToken = default)
    {
        var member = await _staff.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (member is null)
        {
            return Result.Failure(ErrorCodes.Staff.NotFound);
        }

        member.Activate();
        _staff.Update(member);
        _throttle.Reset(member.UserName);
        _throttle.Reset(StaffAuthService.ChangeKey(member.Id));
        _recorder.ByCaller(StaffAuditAction.AccountEnabled, StaffAuditRecorder.Success, member);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ResetPasswordAsync(
        int id,
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (id == _context.StaffId)
        {
            return Result.Failure(ErrorCodes.Staff.CannotResetSelf);
        }

        var member = await _staff.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return member is null
            ? Result.Failure(ErrorCodes.Staff.NotFound)
            : await ApplyResetAsync(member, request.NewPassword, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<StaffAuditPageDto>> ListAuditAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var size = Math.Clamp(pageSize, 1, MaxAuditPageSize);
        var number = Math.Max(page, 1);
        var events = await _audit.ListAsync((number - 1) * size, size, cancellationToken).ConfigureAwait(false);
        var total = await _audit.CountAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<StaffAuditEventDto> items =
        [
            .. events.Select(auditEvent => new StaffAuditEventDto(
                auditEvent.OccurredAt,
                auditEvent.ActorName,
                auditEvent.Action,
                auditEvent.TargetName,
                auditEvent.Outcome,
                auditEvent.RemoteAddress)),
        ];
        return Result<StaffAuditPageDto>.Success(new StaffAuditPageDto(items, number, size, total));
    }

    private async Task<Result> ApplyResetAsync(StaffMember member, string? password, CancellationToken cancellationToken)
    {
        var code = PasswordPolicy.Check(password, member.UserName);
        if (code is not null)
        {
            return Result.Failure(code);
        }

        try
        {
            member.ResetPassword(_hasher.Hash(password!));
        }
        catch (DomainException exception)
        {
            return Result.FromCodes(exception.Codes);
        }

        _staff.Update(member);
        _throttle.Reset(member.UserName);
        _throttle.Reset(StaffAuthService.ChangeKey(member.Id));
        _recorder.ByCaller(StaffAuditAction.PasswordReset, StaffAuditRecorder.Success, member);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
