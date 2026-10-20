namespace NFHotel.Application.Dto.AiDefaults;

/// <summary>The default AI model of one purpose (ADR-0014).</summary>
/// <param name="Purpose">What it is the default for: <c>website</c> or <c>market_analysis</c>.</param>
/// <param name="Model">The model name.</param>
/// <param name="SetBy">The user name of the staff member who set it.</param>
/// <param name="SetAt">When it was set.</param>
/// <param name="SourceRunId">The benchmark run of the choice, or <c>null</c> when it was typed in.</param>
public sealed record AiDefaultDto(string Purpose, string Model, string SetBy, DateTimeOffset SetAt, Guid? SourceRunId);
