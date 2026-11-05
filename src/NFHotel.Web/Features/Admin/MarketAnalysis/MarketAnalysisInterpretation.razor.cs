using Microsoft.AspNetCore.Components;

using NFHotel.Application.Dto.MarketAnalysis;

namespace NFHotel.Web.Features.Admin.MarketAnalysis;

/// <summary>
/// The code behind the overall interpretation: the texts of its labels and the interpretation it shows.
/// </summary>
public partial class MarketAnalysisInterpretation : ComponentBase
{
    /// <summary>The id of the heading, which the contents list links to.</summary>
    public const string SectionId = "overall-title";

    /// <summary>The label of a text that an AI model wrote.</summary>
    public const string AiLabel = "AI-generated";

    /// <summary>The note under the label of an AI text.</summary>
    public const string AiNote = "Written by an AI model from the figures of this report. It is a hypothesis to check, not a forecast.";

    /// <summary>The label of a text that the service calculated from the figures.</summary>
    public const string FiguresLabel = "Calculated from the figures, not by an AI";

    /// <summary>The overall interpretation to show.</summary>
    [Parameter, EditorRequired]
    public MarketAnalysisOverallDto Overall { get; set; } = new(string.Empty, string.Empty, null, string.Empty, null, []);
}
