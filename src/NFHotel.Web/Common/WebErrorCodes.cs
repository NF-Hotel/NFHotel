namespace NFHotel.Web.Common;

/// <summary>
/// Error codes that arise in the Web layer itself.
/// </summary>
/// <remarks>
/// Application owns all business error codes (<c>ErrorCodes</c>). This class holds only
/// the codes Web can produce itself, and today there is exactly one: the unexpected error that
/// <see cref="UseCaseRunner{TService}"/> makes out of an exception, so the screens can treat
/// "the database is down" with the same <c>Result</c> code they treat everything else with.
/// </remarks>
public static class WebErrorCodes
{
    /// <summary>
    /// A use case threw an unexpected exception. The technical detail is logged; the user
    /// gets a neutral message.
    /// </summary>
    public const string Unexpected = "web.unexpected_error";
}
