using NFHotel.Domain.Entities;

namespace NFHotel.Domain.Repositories;

/// <summary>Storage of the default AI model per purpose (ADR-0014).</summary>
public interface IAiDefaultRepository : IRepository<AiDefault, int>
{
    /// <summary>Finds the default of a purpose.</summary>
    /// <param name="purpose">The purpose.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The default, or <c>null</c> when none is set.</returns>
    Task<AiDefault?> FindByPurposeAsync(string purpose, CancellationToken cancellationToken = default);

    /// <summary>Lists every default that is set, ordered by purpose.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The defaults.</returns>
    Task<IReadOnlyList<AiDefault>> ListAsync(CancellationToken cancellationToken = default);
}
