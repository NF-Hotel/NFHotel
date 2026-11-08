using System.Globalization;

using Microsoft.AspNetCore.Components;

using NFHotel.Application.Dto.Benchmark;

namespace NFHotel.Web.Features.Admin.Benchmark;

/// <summary>
/// The code behind one answer card: the texts of an invalid or shortened answer and the number formats.
/// </summary>
public partial class AnswerCard : ComponentBase
{
    private const int RawLimit = 6000;

    /// <summary>The request that the answer belongs to.</summary>
    [Parameter, EditorRequired]
    public BenchmarkResultDto Result { get; set; } = default!;

    /// <summary>The answer text and the reason it was refused, when it was.</summary>
    [Parameter, EditorRequired]
    public BenchmarkOutputDto Output { get; set; } = default!;

    private string InvalidText =>
        string.IsNullOrWhiteSpace(Output.ValidationError) ? "The answer could not be read as the requested structure." : $"Invalid answer: {Output.ValidationError}";

    private string Shortened =>
        Output.Output is { Length: > RawLimit } text ? text[..RawLimit] + "…" : Output.Output ?? string.Empty;

    private static string Percent(double? rate) =>
        rate is { } value ? (value * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%" : "n/a";

    private static string Number(double? value) =>
        value is { } number ? number.ToString("0.##", CultureInfo.InvariantCulture) : "—";
}
