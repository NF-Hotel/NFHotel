using System.ComponentModel.DataAnnotations;

using NFHotel.Domain.Errors;

namespace NFHotel.Domain.Entities;

/// <summary>
/// The default AI model for one purpose (ADR-0014): the website or the market analysis. There is at most one row per
/// purpose. A Manager chooses it from the benchmark results; the services read it.
/// </summary>
public sealed class AiDefault : Entity<int>
{
    /// <summary>The purpose of the AI of the website.</summary>
    public const string Website = "website";

    /// <summary>The purpose of the AI of the market analysis.</summary>
    public const string MarketAnalysis = "market_analysis";

    /// <summary>The longest model name and user name.</summary>
    public const int MaxTextLength = 100;

    /// <summary>The known purposes.</summary>
    public static readonly IReadOnlyList<string> Purposes = [Website, MarketAnalysis];

    private AiDefault()
    {
    }

    /// <summary>What the model is the default for: <see cref="Website"/> or <see cref="MarketAnalysis"/>.</summary>
    [Required(ErrorMessage = AiDefaultErrors.PurposeInvalid)]
    public string Purpose { get; private set; } = string.Empty;

    /// <summary>The model name as the model server knows it, for example <c>llama3.2:3b</c>.</summary>
    [Required(ErrorMessage = AiDefaultErrors.ModelInvalid)]
    [StringLength(MaxTextLength, ErrorMessage = AiDefaultErrors.ModelInvalid)]
    public string Model { get; private set; } = string.Empty;

    /// <summary>The user name of the staff member who set the default.</summary>
    [Required(ErrorMessage = AiDefaultErrors.SetByInvalid)]
    [StringLength(MaxTextLength, ErrorMessage = AiDefaultErrors.SetByInvalid)]
    public string SetBy { get; private set; } = string.Empty;

    /// <summary>When the default was set.</summary>
    public DateTimeOffset SetAt { get; private set; }

    /// <summary>The benchmark run that the choice was made from, or <c>null</c> when it was typed in.</summary>
    public Guid? SourceRunId { get; private set; }

    /// <summary>Creates the default of a purpose.</summary>
    /// <param name="purpose">The purpose.</param>
    /// <param name="model">The model name.</param>
    /// <param name="setBy">Who sets it.</param>
    /// <param name="setAt">When it is set.</param>
    /// <param name="sourceRunId">The benchmark run of the choice, or <c>null</c>.</param>
    /// <returns>The new default.</returns>
    /// <exception cref="DomainException">A field breaks an invariant.</exception>
    public static AiDefault Create(string purpose, string model, string setBy, DateTimeOffset setAt, Guid? sourceRunId)
    {
        var created = new AiDefault();
        created.Apply(purpose, model, setBy, setAt, sourceRunId);

        return created;
    }

    /// <summary>Changes the model of the default.</summary>
    /// <param name="model">The model name.</param>
    /// <param name="setBy">Who changes it.</param>
    /// <param name="setAt">When it is changed.</param>
    /// <param name="sourceRunId">The benchmark run of the choice, or <c>null</c>.</param>
    /// <exception cref="DomainException">A field breaks an invariant; the default is not changed.</exception>
    public void Change(string model, string setBy, DateTimeOffset setAt, Guid? sourceRunId) =>
        Apply(Purpose, model, setBy, setAt, sourceRunId);

    private void Apply(string purpose, string model, string setBy, DateTimeOffset setAt, Guid? sourceRunId)
    {
        var candidate = new AiDefault
        {
            Purpose = purpose ?? string.Empty,
            Model = model?.Trim() ?? string.Empty,
            SetBy = setBy?.Trim() ?? string.Empty,
            SetAt = setAt.ToUniversalTime(),
            SourceRunId = sourceRunId is { } id && id != Guid.Empty ? id : null,
        };

        var codes = new List<string>();
        if (!Purposes.Contains(candidate.Purpose))
        {
            codes.Add(AiDefaultErrors.PurposeInvalid);
        }

        try
        {
            EntityValidation.ThrowIfInvalid(candidate);
        }
        catch (DomainException exception)
        {
            codes.AddRange(exception.Codes);
        }

        if (codes.Count > 0)
        {
            throw new DomainException(codes.Distinct().ToArray());
        }

        Purpose = candidate.Purpose;
        Model = candidate.Model;
        SetBy = candidate.SetBy;
        SetAt = candidate.SetAt;
        SourceRunId = candidate.SourceRunId;
    }
}
