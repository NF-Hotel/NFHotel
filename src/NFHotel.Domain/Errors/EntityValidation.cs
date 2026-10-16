using System.ComponentModel.DataAnnotations;

namespace NFHotel.Domain.Errors;

/// <summary>
/// Runs the validation attributes of an entity and reports every error code at once (BR-60, BR-96).
/// </summary>
/// <remarks>
/// The <see cref="ValidationAttribute.ErrorMessage"/> of each attribute is an error code from
/// <c>NFHotel.Domain.Errors</c>, never a user-facing text (A-09). See ADR-0002.
/// </remarks>
internal static class EntityValidation
{
    /// <summary>
    /// Throws a <see cref="DomainException"/> with every error code when the candidate breaks a rule.
    /// </summary>
    /// <param name="candidate">The entity with the new values already set.</param>
    public static void ThrowIfInvalid(object candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        var results = new List<ValidationResult>();
        var context = new ValidationContext(candidate);

        if (Validator.TryValidateObject(candidate, context, results, validateAllProperties: true))
        {
            return;
        }

        throw new DomainException(results.Select(r => r.ErrorMessage ?? string.Empty).ToArray());
    }
}