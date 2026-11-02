using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Pricing;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Pricing;

namespace NFHotel.ApiClient;

/// <summary>
/// Reads and saves the pricing settings through the API for the web application (MIL-051). The API takes the name of the
/// staff member from the staff token.
/// </summary>
public sealed class ApiPricingSettingsService : IPricingSettingsService
{
    private readonly ApiGateway _api;

    /// <summary>Creates the client.</summary>
    /// <param name="api">The gateway that carries the tokens.</param>
    public ApiPricingSettingsService(ApiGateway api)
    {
        ArgumentNullException.ThrowIfNull(api);
        _api = api;
    }

    /// <inheritdoc />
    public Task<Result<PricingSettingsDto>> GetAsync(CancellationToken cancellationToken = default) =>
        _api.GetAsync<PricingSettingsDto>(ApiRoutes.PricingSettings, cancellationToken);

    /// <inheritdoc />
    public Task<Result<PricingSettingsDto>> SetAsync(SetPricingSettingsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return _api.PutAsync<PricingSettingsDto>(ApiRoutes.PricingSettings, request, cancellationToken);
    }
}
