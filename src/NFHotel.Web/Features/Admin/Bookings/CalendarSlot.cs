using NFHotel.Domain.ValueObjects;

namespace NFHotel.Web.Features.Admin.Bookings;

/// <summary>One room size on one day of the availability calendar: the free rooms out of those that can be sold.</summary>
/// <param name="Size">The room size.</param>
/// <param name="Free">The rooms of the size that are free on the night.</param>
/// <param name="Sellable">The rooms of the size that can be let at all.</param>
public sealed record CalendarSlot(RoomSize Size, int Free, int Sellable)
{
    /// <summary>The colour of the figure.</summary>
    public CalendarTone Tone => CalendarGrid.ToneOf(Free, Sellable);

    /// <summary>The class of the figure in app.css.</summary>
    public string ToneClass => CalendarGrid.ClassOf(Tone);

    /// <summary>The size in a word, as the row of the day shows it.</summary>
    public string Label => Size.ToString();
}
