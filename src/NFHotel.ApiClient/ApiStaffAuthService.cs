using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Staff;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Staff;

namespace NFHotel.ApiClient;

/// <summary><see cref="IStaffAuthService"/> over HTTP: the sign-in goes to the API, which checks the password (ADR-0005).</summary>
public sealed class ApiStaffAuthService : IStaffAuthService
{
    private readonly ApiGateway _api;

    /// <summary>Creates the service.</summary>
    /// <param name="api">The gateway to the API.</param>
    public ApiStaffAuthService(ApiGateway api)
    {
        ArgumentNullException.ThrowIfNull(api);
        _api = api;
    }

    /// <inheritdoc />
    public async Task<Result<StaffSessionDto>> SignInAsync(SignInRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await _api
            .PostAnonymousAsync<StaffSessionDto>($"{ApiRoutes.Auth}/{ApiRoutes.Login}", request, cancellationToken)
            .ConfigureAwait(false);
    }
}