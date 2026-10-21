using NFHotel.Application.Dto.Staff;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Staff;

/// <summary>State of the audit page (UC-006): one page of the trail at a time, newest first.</summary>
public sealed class StaffAuditViewModel
{
    /// <summary>The lines on a page.</summary>
    public const int PageSize = 50;

    private readonly IUseCaseRunner<IStaffAdminService> _admin;

    /// <summary>Creates the view model.</summary>
    /// <param name="admin">Runs the use cases of a Manager.</param>
    public StaffAuditViewModel(IUseCaseRunner<IStaffAdminService> admin)
    {
        ArgumentNullException.ThrowIfNull(admin);

        _admin = admin;
    }

    /// <summary>The lines of the page shown.</summary>
    public IReadOnlyList<StaffAuditEventDto> Items { get; private set; } = Array.Empty<StaffAuditEventDto>();

    /// <summary>The page shown, from 1.</summary>
    public int Page { get; private set; } = 1;

    /// <summary>The number of lines in the whole trail.</summary>
    public int Total { get; private set; }

    /// <summary>The texts of the last refusal, empty when the last call succeeded.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = Array.Empty<string>();

    /// <summary>True once a page has been asked for.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>True when there is an earlier page.</summary>
    public bool HasPrevious => Page > 1;

    /// <summary>True when there is a later page.</summary>
    public bool HasNext => Page * PageSize < Total;

    /// <summary>Loads a page.</summary>
    /// <param name="page">The page, from 1.</param>
    /// <param name="cancellationToken">Cancels the call when the page is closed.</param>
    /// <returns>A task that ends when the page is loaded.</returns>
    public async Task LoadAsync(int page = 1, CancellationToken cancellationToken = default)
    {
        var result = await _admin
            .RunAsync((service, token) => service.ListAuditAsync(Math.Max(page, 1), PageSize, token), cancellationToken)
            .ConfigureAwait(false);
        IsLoaded = true;
        if (result.IsFailure)
        {
            Errors = ErrorMessages.Describe(result);
            return;
        }

        Errors = Array.Empty<string>();
        Items = result.Value.Items;
        Page = result.Value.Page;
        Total = result.Value.Total;
    }
}
