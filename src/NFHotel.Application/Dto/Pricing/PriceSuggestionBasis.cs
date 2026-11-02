namespace NFHotel.Application.Dto.Pricing;

/// <summary>The values of <see cref="PriceSuggestionListDto.Basis"/>.</summary>
public static class PriceSuggestionBasis
{
    /// <summary>The report holds the occupancy by month, so suggestions could be made (the list may still be empty).</summary>
    public const string Ok = "ok";

    /// <summary>There is no usable basis: no completed report, no occupancy analysis, or fewer than twelve calendar months.</summary>
    public const string NoBasis = "no_basis";
}
