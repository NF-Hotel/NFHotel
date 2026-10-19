namespace NFHotel.Application.Dto.AiDefaults;

/// <summary>The body that sets the default AI model of a purpose (ADR-0014).</summary>
/// <param name="Model">The model name.</param>
/// <param name="SourceRunId">The benchmark run of the choice, or <c>null</c> when it was typed in.</param>
public sealed record SetAiDefaultRequest(string Model, Guid? SourceRunId = null);
