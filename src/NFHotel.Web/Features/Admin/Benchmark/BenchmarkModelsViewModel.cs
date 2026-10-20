using NFHotel.Application.Dto.AiDefaults;
using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.AiDefaults;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Benchmark;

/// <summary>
/// State of the list of installed models where a Manager adds a model and deletes one (MIL-020, ADR-0005 of the benchmark
/// service). The page holds only markup and binding; the service checks the name and the use again.
/// </summary>
public sealed class BenchmarkModelsViewModel
{
    private readonly IUseCaseRunner<IBenchmarkServiceClient> _client;
    private readonly IUseCaseRunner<IAiDefaultService> _aiDefaults;

    /// <summary>Creates the view model.</summary>
    /// <param name="client">Runs the calls to the benchmark service.</param>
    /// <param name="aiDefaults">Reads the default AI models, to warn before one of them is deleted.</param>
    public BenchmarkModelsViewModel(IUseCaseRunner<IBenchmarkServiceClient> client, IUseCaseRunner<IAiDefaultService> aiDefaults)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(aiDefaults);

        _client = client;
        _aiDefaults = aiDefaults;
    }

    /// <summary>The installed models, as Ollama lists them.</summary>
    public IReadOnlyList<string> Models { get; private set; } = [];

    /// <summary>The default AI models that are set.</summary>
    public IReadOnlyList<AiDefaultDto> Defaults { get; private set; } = [];

    /// <summary>The text in the field of the model to add.</summary>
    public string NewModelName { get; set; } = string.Empty;

    /// <summary>The download that was started and is shown, or <c>null</c>.</summary>
    public ModelPullDto? CurrentPull { get; private set; }

    /// <summary>The model that waits for the confirmation of its deletion, or <c>null</c>.</summary>
    public string? ModelPendingDeletion { get; private set; }

    /// <summary>The texts of the last refusal, empty when the last call succeeded.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = [];

    /// <summary>True while a call is running.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>True once the models have been asked for, with or without an answer.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>True while a download is queued or running, so the page keeps asking for its state.</summary>
    public bool IsPulling => CurrentPull is { IsFinished: false };

    /// <summary>True when the text in the field is shaped like a model name and nothing else is going on.</summary>
    public bool CanAdd => !IsBusy && !IsPulling && ModelNames.IsValid(NewModelName.Trim());

    /// <summary>Loads the installed models and the defaults when the panel opens, and after a change.</summary>
    /// <param name="cancellationToken">Cancels the call when the page is closed.</param>
    /// <returns>A task that ends when both are loaded.</returns>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsBusy = true;
        try
        {
            var models = await _client
                .RunAsync((client, token) => client.ListModelsAsync(token), cancellationToken)
                .ConfigureAwait(false);

            if (models.IsFailure)
            {
                Errors = ErrorMessages.Describe(models);
                Models = [];
            }
            else
            {
                Errors = [];
                Models = models.Value;
            }

            // A failing read of the defaults only loses the warning before a delete.
            var defaults = await _aiDefaults
                .RunAsync((service, token) => service.GetAllAsync(token), cancellationToken)
                .ConfigureAwait(false);
            Defaults = defaults.IsFailure ? [] : defaults.Value;
        }
        finally
        {
            IsBusy = false;
            IsLoaded = true;
        }
    }

    /// <summary>Starts the download of the model in the field.</summary>
    /// <param name="cancellationToken">Cancels the call when the page is closed.</param>
    /// <returns>A task that ends when the service has answered.</returns>
    public async Task StartPullAsync(CancellationToken cancellationToken = default)
    {
        if (!CanAdd)
        {
            return;
        }

        var name = NewModelName.Trim();
        IsBusy = true;
        try
        {
            var result = await _client
                .RunAsync((client, token) => client.StartModelPullAsync(name, token), cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                return;
            }

            Errors = [];
            CurrentPull = result.Value;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Asks for the state of the download. A finished download reloads the list; a download that the service has forgotten
    /// ends the following.
    /// </summary>
    /// <param name="cancellationToken">Cancels the call when the page is closed.</param>
    /// <returns>A task that ends when the service has answered.</returns>
    public async Task RefreshPullAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentPull is not { } pull)
        {
            return;
        }

        var result = await _client
            .RunAsync((client, token) => client.GetModelPullAsync(pull.PullId, token), cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            Errors = ErrorMessages.Describe(result);
            if (result.Errors.Any(error => error.Code == ErrorCodes.BenchmarkRunner.PullNotFound))
            {
                // The service restarted and forgot the download: there is nothing more to follow.
                CurrentPull = null;
            }

            return;
        }

        Errors = [];
        CurrentPull = result.Value;
        if (result.Value.State == ModelPullStates.Done)
        {
            NewModelName = string.Empty;
            await LoadAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Asks for the confirmation of deleting a model.</summary>
    /// <param name="model">The installed model.</param>
    public void RequestDelete(string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        ModelPendingDeletion = model;
    }

    /// <summary>Backs out of deleting a model.</summary>
    public void AbortDelete() => ModelPendingDeletion = null;

    /// <summary>The purposes that have the model as their default AI model, for the warning before a delete.</summary>
    /// <param name="model">The installed model.</param>
    /// <returns>The words that name the purposes, for example "the website"; empty when it is nobody's default.</returns>
    public IReadOnlyList<string> DefaultPurposesOf(string model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return
        [
            .. Defaults
                .Where(value => string.Equals(value.Model, model, StringComparison.Ordinal)
                    || (!value.Model.Contains(':', StringComparison.Ordinal)
                        && string.Equals(value.Model + ":latest", model, StringComparison.Ordinal)))
                .Select(value => value.Purpose == "website" ? "the website" : "the market analysis"),
        ];
    }

    /// <summary>Deletes the model that the user confirmed, and reads the list again.</summary>
    /// <param name="cancellationToken">Cancels the call when the page is closed.</param>
    /// <returns>A task that ends when the list is read again.</returns>
    public async Task ConfirmDeleteAsync(CancellationToken cancellationToken = default)
    {
        var model = ModelPendingDeletion;
        if (model is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _client
                .RunAsync((client, token) => client.DeleteModelAsync(model, token), cancellationToken)
                .ConfigureAwait(false);

            ModelPendingDeletion = null;
            if (result.IsFailure)
            {
                Errors = ErrorMessages.Describe(result);
                return;
            }

            Errors = [];
        }
        finally
        {
            IsBusy = false;
        }

        await LoadAsync(cancellationToken).ConfigureAwait(false);
    }
}
