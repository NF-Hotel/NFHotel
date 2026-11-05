namespace NFHotel.Web.Features.Home.Neighbourhood;

/// <summary>
/// One place on the map of the neighbourhood on the front page: what it is, how far it is from the front
/// door, and where its pin sits.
/// </summary>
/// <remarks>
/// The map is schematic: the directions are true, the distances are not to scale. A pin sits at
/// <see cref="Bearing"/> degrees from north and <see cref="Radius"/> units from the hotel, inside the
/// 800 × 620 view box of the drawing, so the rings of walking time on the map mean the same for every
/// place. The pin coordinates are integers so the markup needs no number formatting.
/// </remarks>
/// <param name="Id">The key of the place in the address and the markup, for example <c>palace</c>.</param>
/// <param name="Name">The name as the guest reads it.</param>
/// <param name="Mode">How the guest gets there.</param>
/// <param name="Minutes">The time it takes with that <paramref name="Mode"/>, from the front door.</param>
/// <param name="Distance">The distance as text, for example <c>700 m</c>.</param>
/// <param name="Bearing">The compass direction from the hotel, in degrees clockwise from north.</param>
/// <param name="Radius">The distance of the pin from the hotel on the map, in view box units.</param>
/// <param name="Blurb">Two sentences on what the place is.</param>
/// <param name="Tip">The tip reception would give.</param>
/// <param name="MapsQuery">The search text for a map service, so the guest can get directions.</param>
/// <param name="LabelOnLeft">True when the name is written to the left of the pin, so it does not cover its neighbour.</param>
public sealed record NeighbourhoodPlace(
    string Id,
    string Name,
    TravelMode Mode,
    int Minutes,
    string Distance,
    int Bearing,
    int Radius,
    string Blurb,
    string Tip,
    string MapsQuery,
    bool LabelOnLeft = false)
{
    /// <summary>Where the hotel sits in the view box of the map, horizontally.</summary>
    public const int HotelX = 400;

    /// <summary>Where the hotel sits in the view box of the map, vertically.</summary>
    public const int HotelY = 310;

    /// <summary>The horizontal position of the pin in the view box.</summary>
    public int X => HotelX + (int)Math.Round(Radius * Math.Sin(BearingInRadians));

    /// <summary>The vertical position of the pin in the view box. North is up, so a bearing of 0 moves the pin up.</summary>
    public int Y => HotelY - (int)Math.Round(Radius * Math.Cos(BearingInRadians));

    private double BearingInRadians => Bearing * Math.PI / 180;
}
