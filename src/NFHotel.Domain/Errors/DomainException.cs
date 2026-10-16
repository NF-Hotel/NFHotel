namespace NFHotel.Domain.Errors;

/// <summary>
/// Base error for every breach of a domain invariant.
/// </summary>
/// <remarks>
/// The message is always an <b>error code</b>, not a user-facing text (A-09) — for example
/// <c>"guest.first_name_required"</c>. The Application layer translates the code to text,
/// so the UI can show the message in Danish without the domain knowing about localisation.
/// </remarks>
public class DomainException : Exception
{
    /// <summary>
    /// Creates a domain error.
    /// </summary>
    /// <param name="code">The error code, for example <c>"room.floor_invalid"</c>.</param>
    public DomainException(string code)
        : base(code)
    {
        Code = code;
        Codes = [code];
    }

    /// <summary>
    /// Creates a domain error with an underlying error.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="innerException">The underlying error.</param>
    public DomainException(string code, Exception innerException)
        : base(code, innerException)
    {
        Code = code;
        Codes = [code];
    }

    /// <summary>
    /// The machine-readable error code. Identical to <see cref="Exception.Message"/>,
    /// but exposed separately so Application can map without reading a message.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Creates a domain error that carries every error code found, in the order found.
    /// </summary>
    /// <param name="codes">The error codes; at least one.</param>
    public DomainException(IReadOnlyList<string> codes)
        : base(codes.Count > 0 ? codes[0] : string.Empty)
    {
        Codes = codes;
        Code = codes.Count > 0 ? codes[0] : string.Empty;
    }

    /// <summary>
    /// Every error code of this error. Holds one code when the error was made with a single code.
    /// </summary>
    public IReadOnlyList<string> Codes { get; private set; } = [];
}