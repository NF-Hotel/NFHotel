using System.Globalization;

using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Pricing;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Pricing;

namespace NFHotel.ApiClient;

/// <summary>
/// Reads the open price suggestions, approves one, reverts a price change and lists the changes through the API for the web
/// application (MIL-051). The API takes the name of the staff member from the staff token.
/// </summary>
public sealed class ApiPriceSuggestionService : IPriceSuggestionService
{
    private readonly ApiGateway _api;

    /// <summary>Creates the client.</summary>
    /// <param name="api">The gateway that carries the tokens.</param>
    public ApiPriceSuggestionService(ApiGateway api)
    {
        ArgumentNullException.ThrowIfNull(api);
        _api = api;
    }

    /// <inheritdoc />
    public Task<Result<PriceSuggestionListDto>> GetOpenAsync(CancellationToken cancellationToken = default) =>
        _api.GetAsync<PriceSuggestionListDto>(ApiRoutes.PriceSuggestions, cancellationToken);

    /// <inheritdoc />
    public Task<Result<ApprovedPriceChange>> ApproveAsync(PriceSuggestionApproveRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return _api.PostAsync<ApprovedPriceChange>($"{ApiRoutes.PriceSuggestions}/{ApiRoutes.PriceSuggestionApprove}", request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<PriceChangeDto>> RevertAsync(int id, CancellationToken cancellationToken = default) =>
        _api.PostAsync<PriceChangeDto>(
            string.Create(CultureInfo.InvariantCulture, $"{ApiRoutes.PriceChanges}/{id}/revert"),
            null,
            cancellationToken);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<PriceChangeDto>>> GetChangesAsync(CancellationToken cancellationToken = default) =>
        _api.GetAsync<IReadOnlyList<PriceChangeDto>>(ApiRoutes.PriceChanges, cancellationToken);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<EffectivePriceDto>>> GetEffectivePricesAsync(DateOnly? day, CancellationToken cancellationToken = default)
    {
        var path = $"{ApiRoutes.PriceChanges}/{ApiRoutes.PriceChangesEffective}";
        if (day is { } value)
        {
            path += string.Create(CultureInfo.InvariantCulture, $"?day={value:yyyy-MM-dd}");
        }

        return _api.GetAsync<IReadOnlyList<EffectivePriceDto>>(path, cancellationToken);
    }
}
