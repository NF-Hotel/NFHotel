namespace NFHotel.Application.UseCases.Pricing;

/// <summary>The season of a calendar month, judged by its average occupancy (MIL-055).</summary>
public enum Season
{
    /// <summary>Neither high nor low: no price suggestion.</summary>
    Normal = 0,

    /// <summary>The occupancy is at or above the high threshold: a surcharge is suggested.</summary>
    High = 1,

    /// <summary>The occupancy is at or below the low threshold: a discount is suggested.</summary>
    Low = 2
}
