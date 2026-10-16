using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;

namespace NFHotel.TestSupport;

/// <summary>An in-memory store of staff accounts that finds a member by the lower-case user name, as the database index does.</summary>
public sealed class InMemoryStaffRepository : InMemoryRepository<StaffMember>, IStaffRepository
{
    /// <inheritdoc />
    public Task<StaffMember?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(member => member.UserName == StaffMember.NormalizeUserName(userName)));

    /// <inheritdoc />
    public Task<bool> AnyAsync(CancellationToken cancellationToken = default) => Task.FromResult(Items.Count > 0);
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