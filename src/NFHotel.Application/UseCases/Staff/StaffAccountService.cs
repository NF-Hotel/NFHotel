using NFHotel.Application.Dto.Staff;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Staff;

/// <summary>Creates staff accounts (ADR-0005).</summary>
public sealed class StaffAccountService : IStaffAccountService
{
    /// <summary>The shortest password that is accepted.</summary>
    public const int MinPasswordLength = 12;

    private readonly IStaffRepository _staff;
    private readonly IPasswordHasher _hasher;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Creates the service.</summary>
    /// <param name="staff">The staff accounts.</param>
    /// <param name="hasher">Hashes the password.</param>
    /// <param name="unitOfWork">The transaction boundary.</param>
    public StaffAccountService(IStaffRepository staff, IPasswordHasher hasher, IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(staff);
        ArgumentNullException.ThrowIfNull(hasher);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _staff = staff;
        _hasher = hasher;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result> CreateAsync(CreateStaffRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var password = request.Password ?? string.Empty;
        if (password.Length < MinPasswordLength || password.Length > StaffAuthService.MaxPasswordLength)
        {
            return Result.Failure(ErrorCodes.Staff.PasswordInvalid);
        }

        StaffMember member;
        try
        {
            member = StaffMember.Create(request.UserName, _hasher.Hash(password), request.Role);
        }
        catch (DomainException exception)
        {
            return Result.FromCodes(exception.Codes);
        }

        if (await _staff.FindByUserNameAsync(member.UserName, cancellationToken).ConfigureAwait(false) is not null)
        {
            return Result.Failure(ErrorCodes.Staff.UserNameTaken);
        }

        _staff.Add(member);
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
    public async Task<Result> EnsureFirstManagerAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (await _staff.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return Result.Success();
        }

        var created = await CreateAsync(new CreateStaffRequest(userName, password, StaffRole.Manager), cancellationToken)
            .ConfigureAwait(false);

        // A taken name here means another start won the race: the account exists, which is what was asked.
        return created.IsFailure && created.FirstError?.Code == ErrorCodes.Staff.UserNameTaken
            ? Result.Success()
            : created;
    }
}