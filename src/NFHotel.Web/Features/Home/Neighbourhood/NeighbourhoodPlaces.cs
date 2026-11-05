namespace NFHotel.Web.Features.Home.Neighbourhood;

/// <summary>
/// The places around Street 172 that the front page shows on its map, nearest first.
/// </summary>
/// <remarks>
/// Content, not configuration: the places change about as often as the address of the hotel, so they live
/// here rather than in a database table nobody would edit. The places are real; the walking times and the
/// distances are estimates from the front door at a normal pace.
/// <para>
/// TODO: have reception check the walking times before the site goes live.
/// </para>
/// </remarks>
public static class NeighbourhoodPlaces
{
    /// <summary>Every place on the map, in the order of the list under it.</summary>
    public static IReadOnlyList<NeighbourhoodPlace> All { get; } =
    [
        new(
            "quay",
            "The riverside (Sisowath Quay)",
            TravelMode.OnFoot,
            Minutes: 2,
            Distance: "150 m",
            Bearing: 92,
            Radius: 56,
            Blurb: "The promenade along the Tonlé Sap, with cafés, bars and a lot of people out walking in the evening. The breeze makes it the nicest place to be after dark.",
            Tip: "Walk north along the river for the Night Market, south for the Royal Palace.",
            MapsQuery: "Sisowath Quay Phnom Penh"),
        new(
            "ounalom",
            "Wat Ounalom",
            TravelMode.OnFoot,
            Minutes: 3,
            Distance: "200 m",
            Bearing: 50,
            Radius: 66,
            Blurb: "The main temple of Cambodian Buddhism, from the 1440s, at the river end of Street 172. Quiet courtyards and monks going about their day.",
            Tip: "Cover your shoulders and knees, and take your shoes off before the halls.",
            MapsQuery: "Wat Ounalom"),
        new(
            "st178",
            "Street 178 (galleries and silk)",
            TravelMode.OnFoot,
            Minutes: 2,
            Distance: "150 m",
            Bearing: 205,
            Radius: 78,
            Blurb: "One block south of us. Small galleries, silk and silver shops, and the workshops of students from the art university.",
            Tip: "Better for souvenirs than the markets, and nobody hassles you.",
            MapsQuery: "Street 178 Phnom Penh",
            LabelOnLeft: true),
        new(
            "museum",
            "National Museum",
            TravelMode.OnFoot,
            Minutes: 6,
            Distance: "450 m",
            Bearing: 160,
            Radius: 100,
            Blurb: "The red building from 1920 with the best collection of Khmer sculpture there is, around a garden courtyard. Allow an hour or two.",
            Tip: "Go when it opens at 8 am, before the heat and the tour groups.",
            MapsQuery: "National Museum of Cambodia"),
        new(
            "palace",
            "Royal Palace and Silver Pagoda",
            TravelMode.OnFoot,
            Minutes: 9,
            Distance: "700 m",
            Bearing: 176,
            Radius: 150,
            Blurb: "The King's residence. The Throne Hall and the Silver Pagoda, with its floor of silver tiles, are open to visitors most days.",
            Tip: "It closes over lunch. Dress modestly, or rent a cover-up at the gate.",
            MapsQuery: "Royal Palace Phnom Penh"),
        new(
            "nightmarket",
            "Night Market (Phsar Reatrey)",
            TravelMode.OnFoot,
            Minutes: 12,
            Distance: "900 m",
            Bearing: 20,
            Radius: 165,
            Blurb: "An open-air evening market on the riverside. Clothes, crafts and a food court where you eat sitting on mats.",
            Tip: "Busiest at the weekend. Bring small riel notes.",
            MapsQuery: "Phnom Penh Night Market"),
        new(
            "central",
            "Central Market (Psar Thmei)",
            TravelMode.OnFoot,
            Minutes: 15,
            Distance: "1.2 km",
            Bearing: 295,
            Radius: 190,
            Blurb: "The big art-deco dome from 1937. Gold, watches and electronics inside, food and flowers around the edges.",
            Tip: "Haggle politely. Starting at about half the first price is normal.",
            MapsQuery: "Central Market Phnom Penh"),
        new(
            "watphnom",
            "Wat Phnom",
            TravelMode.OnFoot,
            Minutes: 18,
            Distance: "1.4 km",
            Bearing: 345,
            Radius: 215,
            Blurb: "The hill temple the city is named after. The story goes that a woman called Penh found four Buddha statues in a tree washed up by the river in 1372 and built the first shrine here.",
            Tip: "Nicest in the late afternoon. Five minutes by tuk-tuk if it is too hot to walk.",
            MapsQuery: "Wat Phnom"),
        new(
            "independence",
            "Independence Monument",
            TravelMode.OnFoot,
            Minutes: 24,
            Distance: "1.9 km",
            Bearing: 195,
            Radius: 240,
            Blurb: "The lotus-shaped tower from 1958, where Norodom and Sihanouk boulevards meet. Lit up after dark.",
            Tip: "Worth seeing at night. Seven minutes by tuk-tuk.",
            MapsQuery: "Independence Monument Phnom Penh"),
        new(
            "tuolsleng",
            "Tuol Sleng Genocide Museum",
            TravelMode.TukTuk,
            Minutes: 12,
            Distance: "3 km",
            Bearing: 228,
            Radius: 255,
            Blurb: "The former school that was the S-21 prison under the Khmer Rouge. Hard, but important. The audio guide is good.",
            Tip: "Set aside two hours and go in the morning.",
            MapsQuery: "Tuol Sleng Genocide Museum"),
        new(
            "russian",
            "Russian Market (Toul Tom Poung)",
            TravelMode.TukTuk,
            Minutes: 15,
            Distance: "4 km",
            Bearing: 247,
            Radius: 270,
            Blurb: "A covered maze of stalls. Clothes, fabrics, carvings, and some of the best cheap lunch places in town in the middle.",
            Tip: "Go hungry.",
            MapsQuery: "Russian Market Phnom Penh")
    ];
}
