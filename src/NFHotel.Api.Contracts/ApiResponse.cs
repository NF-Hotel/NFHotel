namespace NFHotel.Api.Contracts;

/// <summary>
/// The body of every response of the API: the value on success, or the error codes of the use case.
/// The HTTP status says which kind of failure it is; the codes say which rule.
/// </summary>
/// <typeparam name="TValue">The type of the value, typically a DTO.</typeparam>
/// <param name="Value">The value, or <c>null</c> when the call failed.</param>
/// <param name="Errors">The error codes, empty when the call succeeded.</param>
public sealed record ApiResponse<TValue>(TValue? Value, IReadOnlyList<string> Errors);

/// <summary>The body of a response that carries no value: only the error codes, empty on success.</summary>
/// <param name="Errors">The error codes, empty when the call succeeded.</param>
public sealed record ApiResponse(IReadOnlyList<string> Errors);