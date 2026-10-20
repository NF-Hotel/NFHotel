using System.ComponentModel.DataAnnotations;

using NFHotel.Domain.Errors;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Entities;

/// <summary>
/// One request to one model in a benchmark run: the measurements and the outcome (ADR-0013). A result belongs to its
/// <see cref="BenchmarkRun"/> and is created only through <see cref="BenchmarkRun.Create"/>.
/// </summary>
public sealed class BenchmarkResult : Entity<int>
{
    /// <summary>The longest model, backend, task and category name.</summary>
    public const int MaxNameLength = 100;

    /// <summary>The longest finish reason.</summary>
    public const int MaxFinishReasonLength = 50;

    /// <summary>The longest answer, error or validation text that is stored.</summary>
    public const int MaxTextLength = 1_000_000;

    /// <summary>The analysis type of the hotel cancellation analysis.</summary>
    public const string HotelCancellation = "hotel_cancellation";

    /// <summary>The analysis type of the deterministic tasks.</summary>
    public const string DeterministicTask = "deterministic_task";

    private const string AnalysisTypePattern = "^(hotel_cancellation|deterministic_task)$";

    private BenchmarkResult()
    {
    }

    /// <summary>The key of the run that this request belongs to.</summary>
    public int BenchmarkRunId { get; private set; }

    /// <summary>When the request was made.</summary>
    public DateTimeOffset Timestamp { get; private set; }

    /// <summary>The model server.</summary>
    [Required(ErrorMessage = BenchmarkErrors.BackendInvalid)]
    [StringLength(MaxNameLength, ErrorMessage = BenchmarkErrors.BackendInvalid)]
    public string Backend { get; private set; } = string.Empty;

    /// <summary>The model name.</summary>
    [Required(ErrorMessage = BenchmarkErrors.ModelInvalid)]
    [StringLength(MaxNameLength, ErrorMessage = BenchmarkErrors.ModelInvalid)]
    public string Model { get; private set; } = string.Empty;

    /// <summary>Either <see cref="HotelCancellation"/> or <see cref="DeterministicTask"/>.</summary>
    [Required(ErrorMessage = BenchmarkErrors.AnalysisTypeInvalid)]
    [RegularExpression(AnalysisTypePattern, ErrorMessage = BenchmarkErrors.AnalysisTypeInvalid)]
    public string AnalysisType { get; private set; } = string.Empty;

    /// <summary>The task, or <c>null</c>.</summary>
    [StringLength(MaxNameLength, ErrorMessage = BenchmarkErrors.ModelInvalid)]
    public string? TaskId { get; private set; }

    /// <summary>The task category, or <c>null</c>.</summary>
    [StringLength(MaxNameLength, ErrorMessage = BenchmarkErrors.ModelInvalid)]
    public string? Category { get; private set; }

    /// <summary>The repetition of the request, from 0.</summary>
    [Range(0, int.MaxValue, ErrorMessage = BenchmarkErrors.RunNumberInvalid)]
    public int RunNumber { get; private set; }

    /// <summary>True for the first request to a model in a session, when the model was not yet loaded.</summary>
    public bool Cold { get; private set; }

    /// <summary>Seconds to the first token, or <c>null</c>.</summary>
    [Range(0d, double.MaxValue, ErrorMessage = BenchmarkErrors.MeasurementInvalid)]
    public double? TimeToFirstTokenSeconds { get; private set; }

    /// <summary>Seconds for the whole request.</summary>
    [Range(0d, double.MaxValue, ErrorMessage = BenchmarkErrors.MeasurementInvalid)]
    public double TotalSeconds { get; private set; }

    /// <summary>Tokens in the answer.</summary>
    [Range(0, int.MaxValue, ErrorMessage = BenchmarkErrors.MeasurementInvalid)]
    public int CompletionTokens { get; private set; }

    /// <summary>Tokens in the prompt, or <c>null</c>.</summary>
    [Range(0, int.MaxValue, ErrorMessage = BenchmarkErrors.MeasurementInvalid)]
    public int? PromptTokens { get; private set; }

    /// <summary>Tokens per second, or <c>null</c>.</summary>
    [Range(0d, double.MaxValue, ErrorMessage = BenchmarkErrors.MeasurementInvalid)]
    public double? TokensPerSecond { get; private set; }

    /// <summary>True when the server gave no token count and it was estimated.</summary>
    public bool TokensEstimated { get; private set; }

    /// <summary>Why the answer ended, or <c>null</c>.</summary>
    [StringLength(MaxFinishReasonLength, ErrorMessage = BenchmarkErrors.MeasurementInvalid)]
    public string? FinishReason { get; private set; }

    /// <summary>System memory in use before the request, in megabytes, or <c>null</c>.</summary>
    [Range(0d, double.MaxValue, ErrorMessage = BenchmarkErrors.MeasurementInvalid)]
    public double? RamBeforeMb { get; private set; }

    /// <summary>System memory in use after the request, in megabytes, or <c>null</c>.</summary>
    [Range(0d, double.MaxValue, ErrorMessage = BenchmarkErrors.MeasurementInvalid)]
    public double? RamAfterMb { get; private set; }

    /// <summary>Peak system memory in use, in megabytes, or <c>null</c>.</summary>
    [Range(0d, double.MaxValue, ErrorMessage = BenchmarkErrors.MeasurementInvalid)]
    public double? RamPeakMb { get; private set; }

    /// <summary>GPU memory in use before the request, in megabytes, or <c>null</c>.</summary>
    [Range(0d, double.MaxValue, ErrorMessage = BenchmarkErrors.MeasurementInvalid)]
    public double? VramBeforeMb { get; private set; }

    /// <summary>GPU memory in use after the request, in megabytes, or <c>null</c>.</summary>
    [Range(0d, double.MaxValue, ErrorMessage = BenchmarkErrors.MeasurementInvalid)]
    public double? VramAfterMb { get; private set; }

    /// <summary>Peak GPU memory in use, in megabytes, or <c>null</c>.</summary>
    [Range(0d, double.MaxValue, ErrorMessage = BenchmarkErrors.MeasurementInvalid)]
    public double? VramPeakMb { get; private set; }

    /// <summary>Whether a deterministic task passed, or <c>null</c>.</summary>
    public bool? Passed { get; private set; }

    /// <summary>Whether the answer had the required JSON shape, or <c>null</c>.</summary>
    public bool? ResponseValid { get; private set; }

    /// <summary>Why the answer was invalid, or <c>null</c>.</summary>
    [StringLength(MaxTextLength, ErrorMessage = BenchmarkErrors.MeasurementInvalid)]
    public string? ValidationError { get; private set; }

    /// <summary>The answer text, or <c>null</c>. The list of runs never reads it.</summary>
    [StringLength(MaxTextLength, ErrorMessage = BenchmarkErrors.MeasurementInvalid)]
    public string? Output { get; private set; }

    /// <summary>The error of the request, or <c>null</c>.</summary>
    [StringLength(MaxTextLength, ErrorMessage = BenchmarkErrors.MeasurementInvalid)]
    public string? Error { get; private set; }

    /// <summary>Creates a result from its data and checks every field.</summary>
    /// <param name="data">The measurements and outcome.</param>
    /// <returns>The new result.</returns>
    /// <exception cref="DomainException">A field breaks an invariant.</exception>
    internal static BenchmarkResult Create(BenchmarkResultData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var result = new BenchmarkResult
        {
            Timestamp = data.Timestamp.ToUniversalTime(),
            Backend = data.Backend ?? string.Empty,
            Model = data.Model ?? string.Empty,
            AnalysisType = data.AnalysisType ?? string.Empty,
            TaskId = data.TaskId,
            Category = data.Category,
            RunNumber = data.RunNumber,
            Cold = data.Cold,
            TimeToFirstTokenSeconds = data.TimeToFirstTokenSeconds,
            TotalSeconds = data.TotalSeconds,
            CompletionTokens = data.CompletionTokens,
            PromptTokens = data.PromptTokens,
            TokensPerSecond = data.TokensPerSecond,
            TokensEstimated = data.TokensEstimated,
            FinishReason = data.FinishReason,
            RamBeforeMb = data.RamBeforeMb,
            RamAfterMb = data.RamAfterMb,
            RamPeakMb = data.RamPeakMb,
            VramBeforeMb = data.VramBeforeMb,
            VramAfterMb = data.VramAfterMb,
            VramPeakMb = data.VramPeakMb,
            Passed = data.Passed,
            ResponseValid = data.ResponseValid,
            ValidationError = data.ValidationError,
            Output = data.Output,
            Error = data.Error,
        };

        EntityValidation.ThrowIfInvalid(result);
        return result;
    }
}
