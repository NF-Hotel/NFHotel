namespace NFHotel.Application.Ports;

/// <summary>
/// Where the Application layer writes a warning about something it chose to survive, such as a price that could not be stored
/// for a booking that was saved. The Application layer has no logging package; Infrastructure writes to the host's log.
/// </summary>
/// <remarks>
/// The message must never hold a token, a password or personal data of a guest: an id and the type of an exception are enough.
/// </remarks>
public interface IWarningLog
{
    /// <summary>Writes a warning.</summary>
    /// <param name="message">The text, without personal data or secrets.</param>
    void Warn(string message);
}
