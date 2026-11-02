using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.UseCases.Pricing;

/// <summary>Reads a room size from its name.</summary>
internal static class RoomSizeText
{
    /// <summary>Reads a size name in any letter case, with spaces around it allowed.</summary>
    /// <param name="text">The text.</param>
    /// <param name="size">The size when the text is one of the names.</param>
    /// <returns><c>true</c> when the text is a name of a size; a number never is.</returns>
    public static bool TryParse(string? text, out RoomSize size)
    {
        size = default;
        var trimmed = text?.Trim();

        // Only the names count: Enum.TryParse would also accept "1" or "7".
        return !string.IsNullOrEmpty(trimmed)
            && Enum.GetNames<RoomSize>().Any(name => string.Equals(name, trimmed, StringComparison.OrdinalIgnoreCase))
            && Enum.TryParse(trimmed, ignoreCase: true, out size);
    }
}
