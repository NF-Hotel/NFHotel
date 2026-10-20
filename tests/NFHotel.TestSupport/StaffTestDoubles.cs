using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.TestSupport;

/// <summary>An in-memory store of staff accounts that finds a member by the lower-case user name, as the database index does.</summary>
public sealed class InMemoryStaffRepository : InMemoryRepository<StaffMember>, IStaffRepository
{
    /// <inheritdoc />
    public Task<StaffMember?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(member => member.UserName == StaffMember.NormalizeUserName(userName)));

    /// <inheritdoc />
    public Task<bool> AnyAsync(CancellationToken cancellationToken = default) => Task.FromResult(Items.Count > 0);

    /// <inheritdoc />
    public Task<IReadOnlyList<StaffMember>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StaffMember>>([.. Items.OrderBy(member => member.UserName, StringComparer.Ordinal)]);

    /// <inheritdoc />
    public Task<bool> AnotherActiveManagerExistsAsync(int excludingId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(member => member.Id != excludingId && member.IsActive && member.Role == StaffRole.Manager));
}

/// <summary>An in-memory audit trail that keeps what was added, newest first when listed.</summary>
public sealed class InMemoryStaffAuditRepository : IStaffAuditRepository
{
    /// <summary>Every record that was added, in the order added.</summary>
    public List<StaffAuditEvent> Events { get; } = [];

    /// <inheritdoc />
    public void Add(StaffAuditEvent auditEvent) => Events.Add(auditEvent);

    /// <inheritdoc />
    public Task<IReadOnlyList<StaffAuditEvent>> ListAsync(int skip, int take, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StaffAuditEvent>>([.. Enumerable.Reverse(Events).Skip(skip).Take(take)]);

    /// <inheritdoc />
    public Task<int> CountAsync(CancellationToken cancellationToken = default) => Task.FromResult(Events.Count);
}

/// <summary>A call context that a test fills in: who is calling and from where.</summary>
public sealed class FakeStaffCallContext : IStaffCallContext
{
    /// <inheritdoc />
    public int? StaffId { get; set; }

    /// <inheritdoc />
    public string? StaffName { get; set; }

    /// <inheritdoc />
    public string? RemoteAddress { get; set; }
}

/// <summary>A password hasher without cost: the hash is the password behind a fixed prefix, so a test can see what was stored.</summary>
public sealed class FakePasswordHasher : IPasswordHasher
{
    /// <summary>The prefix of every hash this fake makes.</summary>
    public const string Prefix = "fake-hash:";

    /// <inheritdoc />
    public string Hash(string password) => Prefix + password;

    /// <inheritdoc />
    public bool Verify(string? hash, string password) => hash is not null && hash == Prefix + password;
}

/// <summary>A token issuer that hands out numbered tokens and lets the test set the expiry.</summary>
public sealed class FakeStaffTokenIssuer : IStaffTokenIssuer
{
    private int _issued;

    /// <summary>When the tokens expire.</summary>
    public DateTimeOffset ExpiresAt { get; set; } = new(2030, 1, 1, 20, 0, 0, TimeSpan.Zero);

    /// <inheritdoc />
    public IssuedStaffToken Issue(StaffMember member)
    {
        ArgumentNullException.ThrowIfNull(member);

        _issued++;
        return new IssuedStaffToken($"token-{_issued}-{member.UserName}-{member.Role}", ExpiresAt);
    }
}
