namespace NFHotel.Application.Errors;

/// <summary>
/// En unik-constraint blev brudt — fx to rum med samme rumnummer (BR-N-01).
/// </summary>
/// <remarks>
/// Oversat fra <c>SQLSTATE 23505</c> i Infrastructure. Servicen tjekker eksplicit inden
/// den skriver; denne fejl er kapløbsværnet bag det tjek.
/// </remarks>
public sealed class UniqueConstraintViolationException : Exception
{
    /// <summary>Opretter fejlen.</summary>
    /// <param name="message">Teknisk besked til loggen.</param>
    /// <param name="constraintName">Navnet på den brudte constraint. Bruges som diskriminator.</param>
    public UniqueConstraintViolationException(string message, string constraintName)
        : base(message)
    {
        ConstraintName = constraintName;
    }

    /// <summary>Opretter fejlen med en underliggende databasefejl.</summary>
    /// <param name="message">Teknisk besked til loggen.</param>
    /// <param name="constraintName">Navnet på den brudte constraint.</param>
    /// <param name="innerException">Den underliggende fejl fra dataadgangslaget.</param>
    public UniqueConstraintViolationException(string message, string constraintName, Exception innerException)
        : base(message, innerException)
    {
        ConstraintName = constraintName;
    }

    /// <summary>Navnet på den constraint der blev brudt, fx <c>ux_room_room_number</c>.</summary>
    public string ConstraintName { get; }
}