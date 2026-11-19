using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace NFHotel.Api.Security;

/// <summary>
/// The rules that every value bound from a request meets before an action runs (MIL-077): text holds no control
/// character other than tab, line feed and carriage return, no bidirectional embedding, override or isolate
/// character and no lone surrogate; an enum value is one that its enum defines; a date lies from
/// <see cref="EarliestDate"/> to <see cref="LatestDate"/>; and a text holds at most the length that its property or
/// parameter sets, or <see cref="DefaultMaxTextLength"/> when it sets none (MIL-086).
/// </summary>
/// <remarks>
/// PostgreSQL cannot store a NUL character in text, so a NUL that reached the database would fail the call with a
/// fault instead of a refusal. A bidirectional override makes text read differently from what it holds
/// (CVE-2021-42574). An enum value that its enum does not define, or a date far from any date of the hotel, passes
/// the binding and would reach the use cases as a value that no rule expects. The rules hold for every endpoint, so
/// a new endpoint has them without a change here.
/// </remarks>
public static class InputRules
{
    /// <summary>The earliest date that a request may carry: before any date of birth of a guest.</summary>
    public static readonly DateOnly EarliestDate = new(1900, 1, 1);

    /// <summary>The latest date that a request may carry: far beyond any booking, price or report.</summary>
    public static readonly DateOnly LatestDate = new(2199, 12, 31);

    /// <summary>
    /// The most characters that a text may hold when neither its property nor its parameter sets a length of its own
    /// (MIL-086, ADR-0050 decision 4).
    /// </summary>
    public const int DefaultMaxTextLength = 1000;

    // The objects of a request are shallow; the limit only guards against a type that refers to itself.
    private const int MaxDepth = 8;

    private static readonly ConcurrentDictionary<Type, (PropertyInfo Property, int MaxTextLength)[]> PropertiesByType = new();

    /// <summary>Tells whether a bound value, and every value inside it, meets the rules.</summary>
    /// <param name="value">The value of an action argument.</param>
    /// <returns><see langword="true"/> when the value meets every rule.</returns>
    public static bool IsAccepted(object? value) => IsAccepted(value, 0, DefaultMaxTextLength);

    /// <summary>Tells whether a bound value meets the rules, with the attributes of the parameter that it binds to.</summary>
    /// <param name="value">The value of an action argument.</param>
    /// <param name="declaredOn">The parameter of the action, whose attributes may set the length of a text, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the value meets every rule.</returns>
    public static bool IsAccepted(object? value, ICustomAttributeProvider? declaredOn) =>
        IsAccepted(value, 0, (declaredOn is null ? null : OwnLength(declaredOn)) ?? DefaultMaxTextLength);

    /// <summary>Tells whether text holds only characters that the API accepts.</summary>
    /// <param name="text">The text.</param>
    /// <returns><see langword="true"/> when the text holds no refused character and no lone surrogate.</returns>
    public static bool IsAcceptedText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        for (int index = 0; index < text.Length; index++)
        {
            char character = text[index];
            if (char.IsHighSurrogate(character) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
            {
                index++;
                continue;
            }

            if (char.IsSurrogate(character) || IsRefusedControl(character) || IsBidirectionalControl(character))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Tells whether a date lies from <see cref="EarliestDate"/> to <see cref="LatestDate"/>.</summary>
    /// <param name="date">The date.</param>
    /// <returns><see langword="true"/> when the date is in the range.</returns>
    public static bool IsAcceptedDate(DateOnly date) => date >= EarliestDate && date <= LatestDate;

    // The items of a list have no declaration of their own, so a text among them has the default length.
    private static bool IsAccepted(object? value, int depth, int maxTextLength) => value switch
    {
        null => true,
        string text => text.Length <= maxTextLength && IsAcceptedText(text),
        Enum member => Enum.IsDefined(member.GetType(), member),
        DateOnly date => IsAcceptedDate(date),
        DateTime moment => IsAcceptedDate(DateOnly.FromDateTime(moment)),
        DateTimeOffset moment => IsAcceptedDate(DateOnly.FromDateTime(moment.UtcDateTime)),
        IEnumerable items => depth < MaxDepth
            && items.Cast<object?>().All(item => IsAccepted(item, depth + 1, DefaultMaxTextLength)),
        _ when IsRequestObject(value.GetType()) => depth < MaxDepth
            && PropertiesOf(value.GetType()).All(entry => IsAccepted(entry.Property.GetValue(value), depth + 1, entry.MaxTextLength)),
        _ => true,
    };

    // The length that a StringLength or a MaxLength sets; a MaxLength without a number sets none of its own.
    private static int? OwnLength(ICustomAttributeProvider declaredOn)
    {
        foreach (var attribute in declaredOn.GetCustomAttributes(true))
        {
            switch (attribute)
            {
                case StringLengthAttribute length:
                    return length.MaximumLength;
                case MaxLengthAttribute { Length: >= 0 } length:
                    return length.Length;
            }
        }

        return null;
    }

    private static bool IsRefusedControl(char character) =>
        char.IsControl(character) && character is not '\t' and not '\n' and not '\r';

    // The embedding and override characters U+202A to U+202E and the isolate characters U+2066 to U+2069.
    private static bool IsBidirectionalControl(char character) =>
        character is >= '\u202A' and <= '\u202E' or >= '\u2066' and <= '\u2069';

    // The objects that a request binds to are the records of this solution; a type of the framework is not walked.
    private static bool IsRequestObject(Type type) =>
        type.Namespace?.StartsWith("NFHotel.", StringComparison.Ordinal) == true;

    // A record keeps the attributes of a positional member on the parameter of its constructor, so the length of a
    // property is looked for on the property and then on the parameter of the same name.
    private static (PropertyInfo Property, int MaxTextLength)[] PropertiesOf(Type type) =>
        PropertiesByType.GetOrAdd(type, static key =>
        {
            var parameters = key.GetConstructors().SelectMany(constructor => constructor.GetParameters()).ToArray();

            return key
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
                .Select(property => (property, OwnLength(property)
                    ?? parameters.Where(parameter => parameter.Name == property.Name).Select(OwnLength).FirstOrDefault(length => length is not null)
                    ?? DefaultMaxTextLength))
                .ToArray();
        });
}
