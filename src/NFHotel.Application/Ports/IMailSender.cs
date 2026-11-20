namespace NFHotel.Application.Ports;

/// <summary>
/// Sends one mail through the relay (MIL-088, ADR-0053 decision 1). The use cases build the mails; the adapter knows the
/// relay, its settings and its encrypted connection.
/// </summary>
public interface IMailSender
{
    #region Public methods

    /// <summary>Sends one mail as plain text in UTF-8.</summary>
    /// <param name="mail">The address it goes to, the subject and the text.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>Whether the relay took the mail, and the reason in a word when it did not.</returns>
    /// <exception cref="ArgumentException">The address or the subject holds a line break (ASVS 1.3.11).</exception>
    /// <remarks>A relay that is down, refuses the mail or does not answer in time is a result, never an exception.</remarks>
    Task<MailSendResult> SendAsync(OutgoingMail mail, CancellationToken cancellationToken = default);

    #endregion Public methods
}
