using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Staff;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Staff;

namespace NFHotel.ApiClient;

/// <summary><see cref="IStaffAdminService"/> over HTTP: the API holds the accounts and checks the role of the caller (ADR-0015).</summary>
public sealed class ApiStaffAdminService : IStaffAdminService
{
    private readonly ApiGateway _api;

    /// <summary>Creates the service.</summary>
    /// <param name="api">The gateway to the API.</param>
    public ApiStaffAdminService(ApiGateway api)
    {
        ArgumentNullException.ThrowIfNull(api);
        _api = api;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<StaffAccountDto>>> ListAsync(CancellationToken cancellationToken = default) =>
        await _api.GetAsync<IReadOnlyList<StaffAccountDto>>(ApiRoutes.Staff, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Result> CreateAsync(CreateStaffRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await _api.PostWithoutValueAsync(ApiRoutes.Staff, request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result> DisableAsync(int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        return await _api.PostWithoutValueAsync($"{ApiRoutes.Staff}/{id}/disable", null, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result> EnableAsync(int id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        return await _api.PostWithoutValueAsync($"{ApiRoutes.Staff}/{id}/enable", null, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result> ResetPasswordAsync(int id, ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ArgumentNullException.ThrowIfNull(request);
        return await _api.PostWithoutValueAsync($"{ApiRoutes.Staff}/{id}/password", request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<StaffAuditPageDto>> ListAuditAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        await _api.GetAsync<StaffAuditPageDto>(
            $"{ApiRoutes.Staff}/{ApiRoutes.StaffAudit}?page={page}&pageSize={pageSize}",
            cancellationToken).ConfigureAwait(false);
}
