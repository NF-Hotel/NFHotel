using NFHotel.Web.Features.Home;

namespace NFHotel.Web.Tests;

/// <summary>The map of the neighbourhood on the front page: the places, the pins and the chosen place.</summary>
public sealed class NeighbourhoodViewModelTests
{
    #region Places

    /// <summary>Equivalence: every place has an id of its own, a name, a tip and a pin inside the drawing.</summary>
    [Fact]
    public void Places_All_AreCompleteAndInsideTheMap()
    {
        var places = NeighbourhoodPlaces.All;

        Assert.NotEmpty(places);
        Assert.Equal(places.Count, places.Select(place => place.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.All(places, place =>
        {
            Assert.False(string.IsNullOrWhiteSpace(place.Name));
            Assert.False(string.IsNullOrWhiteSpace(place.Blurb));
            Assert.False(string.IsNullOrWhiteSpace(place.Tip));
            Assert.False(string.IsNullOrWhiteSpace(place.MapsQuery));
            Assert.True(place.Minutes > 0);
            Assert.InRange(place.X, 20, 780);
            Assert.InRange(place.Y, 20, 600);
        });
    }

    /// <summary>Boundary: a bearing of 0 moves the pin straight up from the hotel, 90 straight right.</summary>
    [Fact]
    public void Pin_Bearing_IsMeasuredClockwiseFromNorth()
    {
        var north = Place(bearing: 0, radius: 100);
        var east = Place(bearing: 90, radius: 100);

        Assert.Equal(NeighbourhoodPlace.HotelX, north.X);
        Assert.Equal(NeighbourhoodPlace.HotelY - 100, north.Y);
        Assert.Equal(NeighbourhoodPlace.HotelX + 100, east.X);
        Assert.Equal(NeighbourhoodPlace.HotelY, east.Y);
    }

    #endregion

    #region Selection

    /// <summary>Positive: the map opens on the hotel, and a known id shows that place.</summary>
    [Fact]
    public void Select_KnownId_ShowsThePlace()
    {
        var viewModel = new NeighbourhoodViewModel();
        Assert.Null(viewModel.Selected);
        Assert.Null(viewModel.Previous);
        Assert.Null(viewModel.Next);

        viewModel.Select("palace");

        Assert.Equal("palace", viewModel.Selected?.Id);
        Assert.True(viewModel.IsSelected(viewModel.Selected!));
        Assert.Equal("m-pin m-pin-selected", viewModel.PinClass(viewModel.Selected!));
        Assert.Equal("m-pin", viewModel.PinClass(viewModel.Places[0]));
        Assert.Equal(viewModel.Places[viewModel.NumberOf(viewModel.Selected!) - 1], viewModel.Selected);
    }

    /// <summary>Negative: an unknown id, or none, shows the hotel again.</summary>
    [Fact]
    public void Select_UnknownId_ShowsTheHotel()
    {
        var viewModel = new NeighbourhoodViewModel();
        viewModel.Select("palace");

        viewModel.Select("nowhere");
        Assert.Null(viewModel.Selected);

        viewModel.Select("palace");
        viewModel.ShowHotel();
        Assert.Null(viewModel.Selected);
    }

    /// <summary>Edge: previous and next wrap round at both ends of the list.</summary>
    [Fact]
    public void Neighbours_AtTheEnds_WrapRound()
    {
        var viewModel = new NeighbourhoodViewModel();
        var first = viewModel.Places[0];
        var last = viewModel.Places[^1];

        viewModel.Select(first.Id);
        Assert.Equal(last.Id, viewModel.Previous?.Id);
        Assert.Equal(viewModel.Places[1].Id, viewModel.Next?.Id);

        viewModel.Select(last.Id);
        Assert.Equal(first.Id, viewModel.Next?.Id);
    }

    /// <summary>Positive: the hotel card lists the four closest places on foot, nearest first.</summary>
    [Fact]
    public void Nearest_ListsFourPlacesOnFoot_NearestFirst()
    {
        var viewModel = new NeighbourhoodViewModel();

        var nearest = viewModel.Nearest;

        Assert.Equal(4, nearest.Count);
        Assert.All(nearest, place => Assert.Equal(TravelMode.OnFoot, place.Mode));
        Assert.Equal(nearest.OrderBy(place => place.Minutes).Select(place => place.Id), nearest.Select(place => place.Id));
    }

    #endregion

    #region Texts

    /// <summary>Equivalence: the time names the way of getting there, and the maps link escapes the query.</summary>
    [Fact]
    public void Texts_TimeAndMapsUrl_AreWrittenForTheGuest()
    {
        var onFoot = Place(bearing: 0, radius: 50, mode: TravelMode.OnFoot, minutes: 6);
        var byTukTuk = Place(bearing: 0, radius: 50, mode: TravelMode.TukTuk, minutes: 12);

        Assert.Equal("6 min on foot", NeighbourhoodViewModel.TimeText(onFoot));
        Assert.Equal("12 min by tuk-tuk", NeighbourhoodViewModel.TimeText(byTukTuk));
        Assert.Equal("https://www.google.com/maps/search/?api=1&query=Wat%20Phnom", NeighbourhoodViewModel.MapsUrl(onFoot));
    }

    /// <summary>Negative: the texts refuse a missing place rather than writing nonsense.</summary>
    [Fact]
    public void Texts_MissingPlace_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => NeighbourhoodViewModel.TimeText(null!));
        Assert.Throws<ArgumentNullException>(() => NeighbourhoodViewModel.MapsUrl(null!));
        Assert.Throws<ArgumentNullException>(() => new NeighbourhoodViewModel().IsSelected(null!));
    }

    #endregion

    private static NeighbourhoodPlace Place(int bearing, int radius, TravelMode mode = TravelMode.OnFoot, int minutes = 5) =>
        new("test", "Wat Phnom", mode, minutes, "1 km", bearing, radius, "A place.", "A tip.", "Wat Phnom");
}
