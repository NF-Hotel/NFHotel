using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using NFHotel.Api.Contracts;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;

namespace NFHotel.ApiClient;

/// <summary>
/// Sends one call to the API and turns the answer back into the <see cref="Result"/> that the use case would
/// have given, so the screens cannot tell the API from the service (ADR-0004). A network failure, a timeout,
/// a refused token and an unreadable answer each get one error code of <c>ErrorCodes.Api</c>.
/// </summary>
public sealed class ApiGateway
{
    private readonly HttpClient _http;

    /// <summary>Creates the gateway.</summary>
    /// <param name="http">The client, with the base address, the token and the timeout set.</param>
    public ApiGateway(HttpClient http)
    {
        ArgumentNullException.ThrowIfNull(http);
        _http = http;
    }

    /// <summary>Gets a resource.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="path">The path below the base address.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The result.</returns>
    public Task<Result<TValue>> GetAsync<TValue>(string path, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Get, path, null, ReadValueAsync<TValue>, Result<TValue>.Failure, cancellationToken);

    /// <summary>Posts a body, or nothing, to an endpoint.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="path">The path below the base address.</param>
    /// <param name="body">The request body, or <c>null</c>.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The result.</returns>
    public Task<Result<TValue>> PostAsync<TValue>(string path, object? body, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, path, body, ReadValueAsync<TValue>, Result<TValue>.Failure, cancellationToken);

    /// <summary>Puts a body to an endpoint.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="path">The path below the base address.</param>
    /// <param name="body">The request body.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The result.</returns>
    public Task<Result<TValue>> PutAsync<TValue>(string path, object body, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Put, path, body, ReadValueAsync<TValue>, Result<TValue>.Failure, cancellationToken);

    /// <summary>Deletes a resource; the answer carries no value.</summary>
    /// <param name="path">The path below the base address.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The result.</returns>
    public Task<Result> DeleteAsync(string path, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Delete, path, null, ReadWithoutValueAsync, Result.Failure, cancellationToken);

    private async Task<TResult> SendAsync<TResult>(
        HttpMethod method,
        string path,
        object? body,
        Func<HttpResponseMessage, CancellationToken, Task<TResult>> read,
        Func<string, TResult> fail,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: ApiJson.Options);
        }

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return fail(ErrorCodes.Api.Unauthorized);
            }

            return (int)response.StatusCode >= 500
                ? fail(ErrorCodes.Api.Unavailable)
                : await read(response, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            return fail(ErrorCodes.Api.Unavailable);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // The timeout of the HttpClient, not a cancel by the caller.
            return fail(ErrorCodes.Api.Unavailable);
        }
    }

    private static async Task<Result<TValue>> ReadValueAsync<TValue>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        ApiResponse<TValue>? envelope;
        try
        {
            envelope = await response.Content
                .ReadFromJsonAsync<ApiResponse<TValue>>(ApiJson.Options, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return Result<TValue>.Failure(ErrorCodes.Api.UnexpectedResponse);
        }

        if (envelope is null)
        {
            return Result<TValue>.Failure(ErrorCodes.Api.UnexpectedResponse);
        }

        if (response.IsSuccessStatusCode)
        {
            return envelope.Value is null
                ? Result<TValue>.Failure(ErrorCodes.Api.UnexpectedResponse)
                : Result<TValue>.Success(envelope.Value);
        }

        return envelope.Errors is { Count: > 0 }
            ? Result<TValue>.FromCodes(envelope.Errors)
            : Result<TValue>.Failure(ErrorCodes.Api.UnexpectedResponse);
    }

    private static async Task<Result> ReadWithoutValueAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        ApiResponse? envelope;
        try
        {
            envelope = await response.Content
                .ReadFromJsonAsync<ApiResponse>(ApiJson.Options, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return Result.Failure(ErrorCodes.Api.UnexpectedResponse);
        }

        if (envelope is null)
        {
            return Result.Failure(ErrorCodes.Api.UnexpectedResponse);
        }

        if (response.IsSuccessStatusCode)
        {
            return Result.Success();
        }

        return envelope.Errors is { Count: > 0 }
            ? Result.FromCodes(envelope.Errors)
            : Result.Failure(ErrorCodes.Api.UnexpectedResponse);
    }
}