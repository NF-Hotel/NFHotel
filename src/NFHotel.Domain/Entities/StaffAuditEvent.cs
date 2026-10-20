using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Entities;

/// <summary>
/// One record of the audit trail of the staff accounts (ADR-0015): who did what to whom, when, and with what result.
/// It is written once and never changed or deleted. It never holds a password, a hash, a token or a stamp.
/// </summary>
public sealed class StaffAuditEvent : Entity<int>
{
    /// <summary>The longest user name, outcome and address that are stored.</summary>
    public const int MaxTextLength = 100;

    private StaffAuditEvent()
    {
    }

    /// <summary>When it happened (UTC).</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>The account that did it, or <c>null</c> when nobody was signed in.</summary>
    public int? ActorId { get; private set; }

    /// <summary>The user name of the actor at that moment, or <c>system</c> for a command on the server, or <c>null</c>.</summary>
    public string? ActorName { get; private set; }

    /// <summary>What happened.</summary>
    public StaffAuditAction Action { get; private set; }

    /// <summary>The account it happened to, or <c>null</c>.</summary>
    public int? TargetId { get; private set; }

    /// <summary>The user name of the target at that moment, or <c>null</c>.</summary>
    public string? TargetName { get; private set; }

    /// <summary><c>Success</c>, or the error code that refused it.</summary>
    public string Outcome { get; private set; } = string.Empty;

    /// <summary>The address the request came from, or <c>null</c> when unknown.</summary>
    public string? RemoteAddress { get; private set; }

    /// <summary>Creates a record. Long texts are cut to the column length.</summary>
    /// <param name="occurredAt">When it happened.</param>
    /// <param name="action">What happened.</param>
    /// <param name="outcome"><c>Success</c> or an error code.</param>
    /// <param name="actorId">The actor, if known.</param>
    /// <param name="actorName">The actor's user name.</param>
    /// <param name="targetId">The target account, if any.</param>
    /// <param name="targetName">The target's user name.</param>
    /// <param name="remoteAddress">The address of the request.</param>
    /// <returns>The record.</returns>
    public static StaffAuditEvent Create(
        DateTimeOffset occurredAt,
        StaffAuditAction action,
        string outcome,
        int? actorId = null,
        string? actorName = null,
        int? targetId = null,
        string? targetName = null,
        string? remoteAddress = null) =>
        new()
        {
            OccurredAt = occurredAt,
            Action = action,
            Outcome = Cut(outcome) ?? string.Empty,
            ActorId = actorId,
            ActorName = Cut(actorName),
            TargetId = targetId,
            TargetName = Cut(targetName),
            RemoteAddress = Cut(remoteAddress),
        };

    private static string? Cut(string? text) =>
        text is null ? null : text.Length <= MaxTextLength ? text : text[..MaxTextLength];
}
