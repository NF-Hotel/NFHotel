using NFHotel.Application.Dto.AiDefaults;
using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Repositories;

namespace NFHotel.Application.UseCases.AiDefaults;

/// <summary>Reads and sets the default AI model per purpose (ADR-0014).</summary>
public sealed class AiDefaultService : IAiDefaultService
{
    private readonly IAiDefaultRepository _defaults;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    /// <summary>Creates the service.</summary>
    /// <param name="defaults">The storage of the defaults.</param>
    /// <param name="unitOfWork">Saves the changes.</param>
    /// <param name="clock">The clock that says when a default was set.</param>
    public AiDefaultService(IAiDefaultRepository defaults, IUnitOfWork unitOfWork, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(defaults);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);

        _defaults = defaults;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AiDefaultDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var defaults = await _defaults.ListAsync(cancellationToken).ConfigureAwait(false);

        return Result<IReadOnlyList<AiDefaultDto>>.Success(defaults.Select(ToDto).ToArray());
    }

    /// <inheritdoc />
    public async Task<Result<AiDefaultDto>> SetAsync(
        string purpose,
        SetAiDefaultRequest request,
        string setBy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = _clock.UtcNow;
        AiDefault stored;
        try
        {
            var existing = AiDefault.Purposes.Contains(purpose)
                ? await _defaults.FindByPurposeAsync(purpose, cancellationToken).ConfigureAwait(false)
                : null;
            if (existing is null)
            {
                stored = AiDefault.Create(purpose, request.Model, setBy, now, request.SourceRunId);
                _defaults.Add(stored);
            }
            else
            {
                existing.Change(request.Model, setBy, now, request.SourceRunId);
                stored = existing;
            }
        }
        catch (DomainException exception)
        {
            return Result<AiDefaultDto>.FromCodes(exception.Codes);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<AiDefaultDto>.Success(ToDto(stored));
    }

    private static AiDefaultDto ToDto(AiDefault value) =>
        new(value.Purpose, value.Model, value.SetBy, value.SetAt, value.SourceRunId);
}
