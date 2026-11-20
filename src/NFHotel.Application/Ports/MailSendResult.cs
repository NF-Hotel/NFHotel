namespace NFHotel.Application.Ports;

/// <summary>Whether the relay took a mail (MIL-088, ADR-0053 decision 5), and why not in a word.</summary>
/// <param name="Sent"><see langword="true"/> when the relay took the mail.</param>
/// <param name="FailureReason">One of <see cref="MailFailureReasons"/> when it did not; never an address or a message of the relay.</param>
public sealed record MailSendResult(bool Sent, string? FailureReason)
{
    #region Properties

    /// <summary>The relay took the mail.</summary>
    public static MailSendResult Success { get; } = new(true, null);

    #endregion Properties

    #region Public methods

    /// <summary>The relay did not take the mail.</summary>
    /// <param name="reason">One of <see cref="MailFailureReasons"/>.</param>
    /// <returns>The failed result.</returns>
    public static MailSendResult Failed(string reason) => new(false, reason);

    #endregion Public methods
}
