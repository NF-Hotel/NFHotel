namespace NFHotel.Domain.Errors;

/// <summary>
/// Thrown when a state transition is illegal from the current state of the entity.
/// </summary>
/// <remarks>
/// Setting a state the entity already has is <b>not</b> illegal for the status
/// methods of the room — it is a no-op (A-08), because the mobile app retries over an unstable network.
/// The error code has the form <c>"&lt;entity&gt;.&lt;transition&gt;.invalid_state"</c>.
/// </remarks>
public sealed class InvalidStateTransitionException : DomainException
{
    private const string CodeSuffix = ".invalid_state";

    /// <summary>
    /// Creates an error for an illegal state transition.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="currentState">The state the transition was attempted from.</param>
    /// <param name="transition">The name of the transition, for example <c>"Confirm"</c>.</param>
    public InvalidStateTransitionException(string code, string currentState, string transition)
        : base(code)
    {
        CurrentState = currentState;
        Transition = transition;
    }

    /// <summary>The state the transition was attempted from. For logging and troubleshooting.</summary>
    public string CurrentState { get; }

    /// <summary>The name of the transition that was rejected. For logging and troubleshooting.</summary>
    public string Transition { get; }

    /// <summary>
    /// Builds an error for a rejected transition.
    /// </summary>
    /// <param name="entity">The name of the entity in the error code, for example <c>"booking"</c>.</param>
    /// <param name="transition">The method name of the transition, for example <c>"Confirm"</c>.</param>
    /// <param name="currentState">The current state.</param>
    /// <returns>An error with the code <c>"&lt;entity&gt;.&lt;transition&gt;.invalid_state"</c>.</returns>
    public static InvalidStateTransitionException For(string entity, string transition, object currentState)
    {
        var code = $"{entity}.{ToSnakeCase(transition)}{CodeSuffix}";
        return new InvalidStateTransitionException(code, currentState.ToString() ?? string.Empty, transition);
    }

    /// <summary>
    /// Translates a PascalCase method name to snake_case, so the error codes have the same
    /// form as the validation codes (<c>"booking.check_in.invalid_state"</c>).
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