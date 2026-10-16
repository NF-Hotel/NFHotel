namespace NFHotel.Domain.Errors;

/// <summary>
/// Basisfejl for alle brud på en domæneinvariant.
/// </summary>
/// <remarks>
/// Beskeden er altid en <b>fejlkode</b>, ikke en brugervendt tekst (A-09) — fx
/// <c>"guest.first_name_required"</c>. Application-laget oversætter kode til tekst,
/// så UI kan vise beskeden på dansk uden at Domain kender til lokalisering.
/// </remarks>
public class DomainException : Exception
{
    /// <summary>
    /// Opretter en domænefejl.
    /// </summary>
    /// <param name="code">Fejlkoden, fx <c>"room.floor_invalid"</c>.</param>
    public DomainException(string code)
        : base(code)
    {
        Code = code;
        Codes = [code];
    }

    /// <summary>
    /// Opretter en domænefejl med en underliggende fejl.
    /// </summary>
    /// <param name="code">Fejlkoden.</param>
    /// <param name="innerException">Den underliggende fejl.</param>
    public DomainException(string code, Exception innerException)
        : base(code, innerException)
    {
        Code = code;
        Codes = [code];
    }

    /// <summary>
    /// Den maskinlæsbare fejlkode. Identisk med <see cref="Exception.Message"/>,
    /// men eksponeret separat så Application kan mappe uden at læse en besked.
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