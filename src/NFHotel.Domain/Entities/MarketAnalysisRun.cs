using System.ComponentModel.DataAnnotations;

using NFHotel.Domain.Errors;

namespace NFHotel.Domain.Entities;

/// <summary>
/// A finished market analysis as the data analysis service delivered it (MIL-026): the header of the run and the
/// content of the report as one JSON document, so that a new chart in the report needs no new column. The run id of the
/// service is stored once.
/// </summary>
public sealed class MarketAnalysisRun : Entity<int>
{
    /// <summary>The longest provider and model name.</summary>
    public const int MaxTextLength = 200;

    private MarketAnalysisRun()
    {
    }

    /// <summary>The id the data analysis service gave the run; the key of idempotency.</summary>
    public Guid RunId { get; private set; }

    /// <summary>The version of the result schema that <see cref="ContentJson"/> follows.</summary>
    public int SchemaVersion { get; private set; }

    /// <summary>When the run started.</summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>When the run ended.</summary>
    public DateTimeOffset FinishedAt { get; private set; }

    /// <summary>The AI provider that wrote the analysis, or <c>null</c> when none was used.</summary>
    [StringLength(MaxTextLength, ErrorMessage = MarketAnalysisErrors.ModelInvalid)]
    public string? Provider { get; private set; }

    /// <summary>The model that wrote the analysis, or <c>null</c> when none was used.</summary>
    [StringLength(MaxTextLength, ErrorMessage = MarketAnalysisErrors.ModelInvalid)]
    public string? Model { get; private set; }

    /// <summary>The summary, the figures and the chart series of the report as JSON.</summary>
    [Required(ErrorMessage = MarketAnalysisErrors.ContentRequired)]
    public string ContentJson { get; private set; } = string.Empty;

    /// <summary>Creates a stored run and checks every field.</summary>
    /// <param name="runId">The id from the data analysis service.</param>
    /// <param name="schemaVersion">The version of the result schema.</param>
    /// <param name="startedAt">When the run started.</param>
    /// <param name="finishedAt">When the run ended.</param>
    /// <param name="provider">The AI provider, or <c>null</c>.</param>
    /// <param name="model">The model, or <c>null</c>.</param>
    /// <param name="contentJson">The content of the report as JSON.</param>
    /// <returns>The new run.</returns>
    /// <exception cref="DomainException">A field breaks an invariant.</exception>
    public static MarketAnalysisRun Create(
        Guid runId,
        int schemaVersion,
        DateTimeOffset startedAt,
        DateTimeOffset finishedAt,
        string? provider,
        string? model,
        string contentJson)
    {
        var run = new MarketAnalysisRun
        {
            RunId = runId,
            SchemaVersion = schemaVersion,
            StartedAt = startedAt.ToUniversalTime(),
            FinishedAt = finishedAt.ToUniversalTime(),
            Provider = provider,
            Model = model,
            ContentJson = contentJson ?? string.Empty,
        };

        var codes = new List<string>();
        if (runId == Guid.Empty)
        {
            codes.Add(MarketAnalysisErrors.RunIdRequired);
        }

        if (finishedAt < startedAt)
        {
            codes.Add(MarketAnalysisErrors.EndBeforeStart);
        }

        try
        {
            EntityValidation.ThrowIfInvalid(run);
        }
        catch (DomainException exception)
        {
            codes.AddRange(exception.Codes);
        }

        if (codes.Count > 0)
        {
            throw new DomainException(codes.Distinct().ToArray());
        }

        return run;
    }
}
