using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace NFHotel.Api.Security;

/// <summary>
/// The rules that every value bound from a request meets before an action runs (MIL-077): text holds no control
/// character other than tab, line feed and carriage return, no bidirectional embedding, override or isolate
/// character and no lone surrogate; an enum value is one that its enum defines; and a date lies from
/// <see cref="EarliestDate"/> to <see cref="LatestDate"/>.
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

    // The objects of a request are shallow; the limit only guards against a type that refers to itself.
    private const int MaxDepth = 8;

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertiesByType = new();

    /// <summary>Tells whether a bound value, and every value inside it, meets the rules.</summary>
    /// <param name="value">The value of an action argument.</param>
    /// <returns><see langword="true"/> when the value meets every rule.</returns>
    public static bool IsAccepted(object? value) => IsAccepted(value, 0);

    /// <summary>Tells whether text holds only characters that the API accepts.</summary>
    /// <param name="text">The text.</param>
    /// <returns><see langword="true"/> when the text holds no refused character and no lone surrogate.</returns>
    public static bool IsAcceptedText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
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

    private static bool IsAccepted(object? value, int depth) => value switch
    {
        null => true,
        string text => IsAcceptedText(text),
        Enum member => Enum.IsDefined(member.GetType(), member),
        DateOnly date => IsAcceptedDate(date),
        DateTime moment => IsAcceptedDate(DateOnly.FromDateTime(moment)),
        DateTimeOffset moment => IsAcceptedDate(DateOnly.FromDateTime(moment.UtcDateTime)),
        IEnumerable items => depth < MaxDepth && items.Cast<object?>().All(item => IsAccepted(item, depth + 1)),
        _ when IsRequestObject(value.GetType()) =>
            depth < MaxDepth && PropertiesOf(value.GetType()).All(property => IsAccepted(property.GetValue(value), depth + 1)),
        _ => true,
    };

    private static bool IsRefusedControl(char character) =>
        char.IsControl(character) && character is not '\t' and not '\n' and not '\r';

    // The embedding and override characters U+202A to U+202E and the isolate characters U+2066 to U+2069.
    private static bool IsBidirectionalControl(char character) =>
        character is >= '\u202A' and <= '\u202E' or >= '\u2066' and <= '\u2069';

    // The objects that a request binds to are the records of this solution; a type of the framework is not walked.
    private static bool IsRequestObject(Type type) =>
        type.Namespace?.StartsWith("NFHotel.", StringComparison.Ordinal) == true;

    private static PropertyInfo[] PropertiesOf(Type type) =>
        PropertiesByType.GetOrAdd(type, static key => key
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
            .ToArray());
}
