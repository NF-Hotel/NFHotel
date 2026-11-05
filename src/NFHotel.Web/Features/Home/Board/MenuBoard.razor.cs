using Microsoft.AspNetCore.Components;

namespace NFHotel.Web.Features.Home.Board;

/// <summary>
/// The code behind the departure board: the two timetables the board shows and the helper that
/// splits a text into one tile per letter.
/// </summary>
public partial class MenuBoard : ComponentBase
{
    /// <summary>Tiles in the time column: <c>18:00</c>.</summary>
    private const int TimeWidth = 5;

    /// <summary>Tiles in the dish column: the longest dish on the board, so every row is as wide as the next.</summary>
    private const int DishWidth = 19;

    private const string Open = "OPEN";

    /// <summary>A tile with nothing on it. A no-break space keeps the span from collapsing.</summary>
    private const string Blank = " ";

    private static readonly (string Time, string Dish)[] Evening =
    [
        ("18:00", "Cambodia Draft"),
        ("19:00", "Chicken Satay"),
        ("20:30", "Cambodian Curry"),
        ("21:00", "Passionfruit Mojito"),
        ("22:00", "Chili Cheese Fries"),
        ("23:30", "Espresso Martini")
    ];

    private static readonly (string Time, string Dish)[] LateNight =
    [
        ("00:00", "Rodeo Bacon Burger"),
        ("01:00", "Blue Margarita"),
        ("02:00", "Mike's Beef Chili"),
        ("03:00", "Mac n Cheese Balls"),
        ("04:00", "Philly Cheesesteak"),
        ("05:00", "Banana Pancakes")
    ];

    /// <summary>
    /// Splits a text into one upper-case letter per tile, padded with blank tiles to the width of the column.
    /// </summary>
    private static IEnumerable<string> Tiles(string text, int width) =>
        text.ToUpperInvariant().PadRight(width)[..width].Select(letter => letter == ' ' ? Blank : letter.ToString());
}
