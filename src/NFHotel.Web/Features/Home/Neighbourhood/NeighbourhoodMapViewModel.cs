using System.Globalization;

namespace NFHotel.Web.Features.Home.Neighbourhood;

/// <summary>
/// The state of the map of the neighbourhood on the front page: which place the guest is looking at.
/// </summary>
/// <remarks>
/// <c>NeighbourhoodMap.razor</c> is markup. It calls the methods below and reads the properties — the
/// choice of place, the order of the places and the texts about them are decided here, so a test can walk
/// the map without a browser (B-10, UI-Spec section 4). No service is involved: the places are content
/// (<see cref="NeighbourhoodPlaces"/>), and nothing about them changes while the guest looks.
/// </remarks>
public sealed class NeighbourhoodMapViewModel
{
    /// <summary>How many of the nearest places the hotel card lists.</summary>
    private const int NearestCount = 4;

    private const string MapsSearch = "https://www.google.com/maps/search/?api=1&query=";

    /// <summary>The places on the map, nearest first. Fixed content.</summary>
    public IReadOnlyList<NeighbourhoodPlace> Places => NeighbourhoodPlaces.All;

    /// <summary>The place the guest is looking at, or <c>null</c> when the card shows the hotel itself.</summary>
    public NeighbourhoodPlace? Selected { get; private set; }

    /// <summary>The place before <see cref="Selected"/> in the list, wrapping round; <c>null</c> while the hotel is shown.</summary>
    public NeighbourhoodPlace? Previous => Neighbour(-1);

    /// <summary>The place after <see cref="Selected"/> in the list, wrapping round; <c>null</c> while the hotel is shown.</summary>
    public NeighbourhoodPlace? Next => Neighbour(1);

    /// <summary>The places closest to the front door on foot, for the card that shows the hotel.</summary>
    public IReadOnlyList<NeighbourhoodPlace> Nearest => Places
        .Where(place => place.Mode == TravelMode.OnFoot)
        .OrderBy(place => place.Minutes)
        .Take(NearestCount)
        .ToArray();

    /// <summary>
    /// Writes how long it takes to get to a place: <c>6 min on foot</c> or <c>12 min by tuk-tuk</c>.
    /// </summary>
    /// <param name="place">The place.</param>
    /// <returns>The English text.</returns>
    public static string TimeText(NeighbourhoodPlace place)
    {
        ArgumentNullException.ThrowIfNull(place);

        var minutes = place.Minutes.ToString(CultureInfo.InvariantCulture);

        return place.Mode == TravelMode.TukTuk
            ? $"{minutes} min by tuk-tuk"
            : $"{minutes} min on foot";
    }

    /// <summary>
    /// The address of a map search for the place, so the guest can get directions on their own phone.
    /// </summary>
    /// <param name="place">The place.</param>
    /// <returns>An https address.</returns>
    public static string MapsUrl(NeighbourhoodPlace place)
    {
        ArgumentNullException.ThrowIfNull(place);

        return MapsSearch + Uri.EscapeDataString(place.MapsQuery);
    }

    /// <summary>
    /// The number of a place as the map and the list show it: its position in the list, from 1.
    /// </summary>
    /// <param name="place">The place.</param>
    /// <returns>1 for the first place.</returns>
    public int NumberOf(NeighbourhoodPlace place) => IndexOf(place) + 1;

    /// <summary>True when the place is the one the card shows.</summary>
    /// <param name="place">The place.</param>
    /// <returns>Whether it is selected.</returns>
    public bool IsSelected(NeighbourhoodPlace place)
    {
        ArgumentNullException.ThrowIfNull(place);

        return Selected is not null && Selected.Id == place.Id;
    }

    /// <summary>
    /// Gives the classes of the pin of a place on the map.
    /// </summary>
    /// <param name="place">The place.</param>
    /// <returns><c>m-pin</c>, with <c>m-pin-selected</c> added for the chosen place.</returns>
    public string PinClass(NeighbourhoodPlace place) => IsSelected(place) ? "m-pin m-pin-selected" : "m-pin";

    /// <summary>
    /// Shows a place on the card. An unknown id, or <c>null</c>, shows the hotel.
    /// </summary>
    /// <param name="id">The id of the place, for example <c>palace</c>.</param>
    public void Select(string? id) => Selected = Places.FirstOrDefault(place => place.Id == id);

    /// <summary>Shows the hotel on the card again.</summary>
    public void ShowHotel() => Selected = null;

    private int IndexOf(NeighbourhoodPlace place)
    {
        ArgumentNullException.ThrowIfNull(place);

        for (var index = 0; index < Places.Count; index++)
        {
            if (Places[index].Id == place.Id)
            {
                return index;
            }
        }

        return -1;
    }

    private NeighbourhoodPlace? Neighbour(int offset)
    {
        if (Selected is null)
        {
            return null;
        }

        var count = Places.Count;
        var index = (IndexOf(Selected) + offset + count) % count;

        return Places[index];
    }
}
