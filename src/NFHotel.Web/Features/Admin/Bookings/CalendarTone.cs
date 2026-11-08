namespace NFHotel.Web.Features.Admin.Bookings;

/// <summary>How many rooms are left, as the colour of a figure in the availability calendar.</summary>
public enum CalendarTone
{
    /// <summary>The hotel has no room of the size that can be sold, so there is nothing to count.</summary>
    None = 0,

    /// <summary>More than one room is free, or the only room of the size is free.</summary>
    Ok = 1,

    /// <summary>The last of several rooms is free.</summary>
    Low = 2,

    /// <summary>Every room is taken.</summary>
    Full = 3,
}
