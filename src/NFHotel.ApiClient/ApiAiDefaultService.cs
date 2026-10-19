using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.AiDefaults;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.AiDefaults;

namespace NFHotel.ApiClient;

/// <summary>
/// Reads and sets the default AI model through the API for the web application (ADR-0014). The API takes the name of
/// the staff member from the staff token, so the <c>setBy</c> argument is not sent.
/// </summary>
public sealed class ApiAiDefaultService : IAiDefaultService
{
    private readonly ApiGateway _api;

    /// <summary>Creates the client.</summary>
    /// <param name="api">The gateway that carries the tokens.</param>
    public ApiAiDefaultService(ApiGateway api)
    {
        ArgumentNullException.ThrowIfNull(api);
        _api = api;
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<AiDefaultDto>>> GetAllAsync(CancellationToken cancellationToken = default) =>
        _api.GetAsync<IReadOnlyList<AiDefaultDto>>(ApiRoutes.AiDefaults, cancellationToken);

    /// <inheritdoc />
    public Task<Result<AiDefaultDto>> SetAsync(
        string purpose,
        SetAiDefaultRequest request,
        string setBy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return _api.PutAsync<AiDefaultDto>($"{ApiRoutes.AiDefaults}/{Uri.EscapeDataString(purpose ?? string.Empty)}", request, cancellationToken);
    }
}
