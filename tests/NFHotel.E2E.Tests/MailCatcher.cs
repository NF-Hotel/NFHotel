using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace NFHotel.E2E.Tests;

/// <summary>
/// The mails that the stack sent, read from the REST API of Mailpit, which catches every mail of the stack with the
/// verification of a booking by email on (MIL-088, ADR-0053): the plain text of the n-th mail with a subject to one
/// address. The relay hands a mail on after the API has answered, so a test waits for it, up to a limit.
/// </summary>
public sealed class MailCatcher : IDisposable
{
    #region Fields

    /// <summary>How long a mail may take from the relay to Mailpit before the test fails.</summary>
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(30);

    /// <summary>How often Mailpit is asked while a test waits.</summary>
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

    private readonly HttpClient _http = new() { BaseAddress = E2ESettings.MailApiUrl, Timeout = TimeSpan.FromSeconds(10) };

    #endregion Fields

    #region Public methods

    /// <summary>Waits for the n-th mail with a subject to an address, counted from the oldest, and reads its plain text.</summary>
    /// <param name="address">The address the mail went to.</param>
    /// <param name="subject">The subject of the mail.</param>
    /// <param name="number">Which mail, from 1 for the first.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <returns>The plain text of the mail.</returns>
    /// <exception cref="TimeoutException">The mail did not come within the patience of the catcher.</exception>
    public async Task<string> WaitForTextAsync(string address, string subject, int number, CancellationToken cancellationToken)
    {
        using CancellationTokenSource patience = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        patience.CancelAfter(Patience);
        using PeriodicTimer poll = new(PollInterval);
        try
        {
            while (true)
            {
                IReadOnlyList<MailSummary> mails = await MailsAsync(address, subject, patience.Token);
                if (mails.Count >= number)
                {
                    MailMessage message = await _http.GetFromJsonAsync<MailMessage>(
                            $"api/v1/message/{Uri.EscapeDataString(mails[number - 1].Id)}", patience.Token)
                        ?? throw new InvalidOperationException("Mailpit answered a message without a body.");
                    return message.Text;
                }

                await poll.WaitForNextTickAsync(patience.Token);
            }
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Mail {number} with the subject '{subject}' did not reach Mailpit within {Patience.TotalSeconds} seconds.",
                exception);
        }
    }

    /// <inheritdoc />
    public void Dispose() => _http.Dispose();

    #endregion Public methods

    #region Private methods

    /// <summary>The mails with a subject to exactly one address, the oldest first.</summary>
    private async Task<IReadOnlyList<MailSummary>> MailsAsync(string address, string subject, CancellationToken cancellationToken)
    {
        string query = Uri.EscapeDataString($"to:\"{address}\"");
        MailList? list = await _http.GetFromJsonAsync<MailList>($"api/v1/search?query={query}&limit=50", cancellationToken);
        return (list?.Messages ?? [])
            .Where(mail => string.Equals(mail.Subject, subject, StringComparison.Ordinal))
            .Where(mail => (mail.To ?? []).Any(to => string.Equals(to.Address, address, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(mail => mail.Created)
            .ToList();
    }

    #endregion Private methods

    #region Nested types

    /// <summary>The answer of the search of Mailpit.</summary>
    private sealed record MailList([property: JsonPropertyName("messages")] IReadOnlyList<MailSummary>? Messages);

    /// <summary>One mail in a list of Mailpit.</summary>
    private sealed record MailSummary(
        [property: JsonPropertyName("ID")] string Id,
        [property: JsonPropertyName("Subject")] string Subject,
        [property: JsonPropertyName("To")] IReadOnlyList<MailRecipient>? To,
        [property: JsonPropertyName("Created")] DateTimeOffset Created);

    /// <summary>One address of a mail.</summary>
    private sealed record MailRecipient([property: JsonPropertyName("Address")] string Address);

    /// <summary>One mail as Mailpit reads it: only its plain text is used.</summary>
    private sealed record MailMessage([property: JsonPropertyName("Text")] string Text);

    #endregion Nested types
}
