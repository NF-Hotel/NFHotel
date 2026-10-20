using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;

namespace NFHotel.TestSupport;

/// <summary>An in-memory repository of the default AI models that finds a default by its purpose.</summary>
public sealed class InMemoryAiDefaultRepository : InMemoryRepository<AiDefault>, IAiDefaultRepository
{
    public Task<AiDefault?> FindByPurposeAsync(string purpose, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(value => value.Purpose == purpose));

    public Task<IReadOnlyList<AiDefault>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AiDefault>>([.. Items.OrderBy(value => value.Purpose, StringComparer.Ordinal)]);
}
