namespace NFHotel.Application.Dto.Pricing;

/// <summary>The values of <see cref="PriceSuggestionListDto.Basis"/>.</summary>
public static class PriceSuggestionBasis
{
    /// <summary>The report holds price suggestions of the service, so the list is the service's (it may still be empty).</summary>
    public const string Ok = "ok";

    /// <summary>There is no usable basis: no completed report, or a report of the service without price suggestions (a result of schema 1.2).</summary>
    public const string NoBasis = "no_basis";
}
