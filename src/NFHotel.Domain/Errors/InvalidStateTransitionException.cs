namespace NFHotel.Domain.Errors;

/// <summary>
/// Kastes når en tilstandsovergang er ulovlig fra entitetens nuværende tilstand.
/// </summary>
/// <remarks>
/// At sætte en tilstand entiteten allerede har er <b>ikke</b> ulovligt for rummets
/// status-metoder — det er en no-op (A-08), fordi mobilappen retry'er over ustabilt netværk.
/// Fejlkoden har formen <c>"&lt;entitet&gt;.&lt;overgang&gt;.invalid_state"</c>.
/// </remarks>
public sealed class InvalidStateTransitionException : DomainException
{
    private const string CodeSuffix = ".invalid_state";

    /// <summary>
    /// Opretter en fejl for en ulovlig tilstandsovergang.
    /// </summary>
    /// <param name="code">Fejlkoden.</param>
    /// <param name="currentState">Tilstanden overgangen blev forsøgt fra.</param>
    /// <param name="transition">Navnet på overgangen, fx <c>"Confirm"</c>.</param>
    public InvalidStateTransitionException(string code, string currentState, string transition)
        : base(code)
    {
        CurrentState = currentState;
        Transition = transition;
    }

    /// <summary>Tilstanden overgangen blev forsøgt fra. Til logning og fejlsøgning.</summary>
    public string CurrentState { get; }

    /// <summary>Navnet på den overgang der blev afvist. Til logning og fejlsøgning.</summary>
    public string Transition { get; }

    /// <summary>
    /// Bygger en fejl for en afvist overgang.
    /// </summary>
    /// <param name="entity">Entitetens navn i fejlkoden, fx <c>"booking"</c>.</param>
    /// <param name="transition">Overgangens metodenavn, fx <c>"Confirm"</c>.</param>
    /// <param name="currentState">Den nuværende tilstand.</param>
    /// <returns>En fejl med koden <c>"&lt;entity&gt;.&lt;transition&gt;.invalid_state"</c>.</returns>
    public static InvalidStateTransitionException For(string entity, string transition, object currentState)
    {
        var code = $"{entity}.{ToSnakeCase(transition)}{CodeSuffix}";
        return new InvalidStateTransitionException(code, currentState.ToString() ?? string.Empty, transition);
    }

    /// <summary>
    /// Oversætter et PascalCase-metodenavn til snake_case, så fejlkoderne har samme
    /// form som valideringskoderne (<c>"booking.check_in.invalid_state"</c>).
    /// </summary>
    private static string ToSnakeCase(string pascalCase)
    {
        var builder = new System.Text.StringBuilder(pascalCase.Length + 8);

        for (var i = 0; i < pascalCase.Length; i++)
        {
            var character = pascalCase[i];

            if (char.IsUpper(character) && i > 0)
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }
}