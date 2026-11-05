using Microsoft.AspNetCore.Components;

using NFHotel.Domain.ValueObjects;

namespace NFHotel.Web.Components.Shared;

/// <summary>The code behind the status badge: the one status it shows.</summary>
public partial class StatusBadge : ComponentBase
{
    /// <summary>The life cycle state of the booking. Set only this if the badge shows a booking.</summary>
    [Parameter]
    public BookingStatus? BookingStatus { get; set; }

    /// <summary>The bookability of the room. Set only this if the badge shows the operating state of a room.</summary>
    [Parameter]
    public RoomStatus? RoomStatus { get; set; }

    /// <summary>The cleaning and service state of the room. Set only this if the badge shows cleaning.</summary>
    [Parameter]
    public HousekeepingStatus? HousekeepingStatus { get; set; }
}
