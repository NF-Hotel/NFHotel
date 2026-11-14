namespace NFHotel.Api;

/// <summary>The error codes that the API itself answers, outside the codes of the use cases.</summary>
public static class ApiErrorCodes
{
    /// <summary>The request cannot be read or holds a value that no endpoint accepts (400).</summary>
    public const string InvalidRequest = "api.invalid_request";

    /// <summary>The API failed; the answer shows nothing of the fault (500).</summary>
    public const string UnexpectedError = "api.unexpected_error";
}
