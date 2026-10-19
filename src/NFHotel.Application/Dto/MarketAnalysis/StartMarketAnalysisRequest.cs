namespace NFHotel.Application.Dto.MarketAnalysis;

/// <summary>
/// What a Manager asks for when starting a market analysis (MIL-026): the period of the bookings to analyse, by arrival date.
/// Both dates are optional and inclusive; leaving one out leaves that side open, and leaving both out analyses every booking.
/// </summary>
/// <param name="From">The first arrival date to include, or <c>null</c> for no lower limit.</param>
/// <param name="To">The last arrival date to include, or <c>null</c> for no upper limit.</param>
public sealed record StartMarketAnalysisRequest(DateOnly? From = null, DateOnly? To = null);
