namespace NFHotel.Application.Ports;

/// <summary>One mail to send (MIL-088, ADR-0053 decision 1): plain text with a fixed subject.</summary>
/// <param name="To">The address it goes to.</param>
/// <param name="Subject">The subject, one line.</param>
/// <param name="Body">The text.</param>
public sealed record OutgoingMail(string To, string Subject, string Body);
