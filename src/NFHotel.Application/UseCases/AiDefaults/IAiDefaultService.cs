using NFHotel.Application.Dto.AiDefaults;

namespace NFHotel.Application.UseCases.AiDefaults;

/// <summary>
/// Reads and sets the default AI model per purpose (ADR-0014). The API implements it with the repository, and the web
/// application with a client over HTTP.
/// </summary>
public interface IAiDefaultService
{
    /// <summary>Lists the defaults that are set.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The defaults, ordered by purpose.</returns>
    Task<Result<IReadOnlyList<AiDefaultDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Sets the default of a purpose; an earlier default of that purpose is replaced.</summary>
    /// <param name="purpose">The purpose: <c>website</c> or <c>market_analysis</c>.</param>
    /// <param name="request">The model and the run it was chosen from.</param>
    /// <param name="setBy">The user name of the staff member who sets it; the API reads it from the staff token.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The default after the change, or the codes that say what is wrong.</returns>
    Task<Result<AiDefaultDto>> SetAsync(
        string purpose,
        SetAiDefaultRequest request,
        string setBy,
        CancellationToken cancellationToken = default);
}
