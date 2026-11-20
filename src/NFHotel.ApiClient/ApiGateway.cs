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
/// a refused token and an unreadable answer each get one error code of <c>ErrorCodes.Api</c>; so does a server error,
/// except the 503 of a mail that could not go (MIL-088, ADR-0053), which keeps its code.
/// </summary>
public sealed class ApiGateway
{
    private readonly HttpClient _http;
    private readonly StaffCallContext? _staff;
    private readonly GuestCallContext? _guest;

    /// <summary>Creates the gateway.</summary>
    /// <param name="http">The client, with the base address, the token and the timeout set.</param>
    /// <param name="staff">Holds the staff token of the call, if a member of staff is signed in (ADR-0005).</param>
    /// <param name="guest">
    /// Holds the guest token of the call, if a guest is signed in (MIL-018). It is sent only when the call has no staff
    /// token, so a call never carries both and the API never has to choose.
    /// </param>
    public ApiGateway(HttpClient http, StaffCallContext? staff = null, GuestCallContext? guest = null)
    {
        ArgumentNullException.ThrowIfNull(http);
        _http = http;
        _staff = staff;
        _guest = guest;
    }

    /// <summary>Gets a resource.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="path">The path below the base address.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The result.</returns>
    public Task<Result<TValue>> GetAsync<TValue>(string path, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Get, path, null, ReadValueAsync<TValue>, Result<TValue>.Failure, cancellationToken);

    /// <summary>Gets a resource that may be absent: a 204 is a successful result with no value.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="path">The path below the base address.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The result; its value is <c>null</c> when the API answered 204.</returns>
    public Task<Result<TValue?>> GetOptionalAsync<TValue>(string path, CancellationToken cancellationToken)
        where TValue : class =>
        SendAsync(HttpMethod.Get, path, null, ReadOptionalValueAsync<TValue>, Result<TValue?>.Failure, cancellationToken);

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

    /// <summary>Posts a body, or nothing, to an endpoint whose answer carries no value.</summary>
    /// <param name="path">The path below the base address.</param>
    /// <param name="body">The request body, or <c>null</c>.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The result.</returns>
    public Task<Result> PostWithoutValueAsync(string path, object? body, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, path, body, ReadWithoutValueAsync, Result.Failure, cancellationToken);

    /// <summary>
    /// Posts a body to an endpoint that answers with a value, without a staff or guest token, such as the staff and the
    /// guest sign-in.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="path">The path below the base address.</param>
    /// <param name="body">The request body.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The result; a 401 or 429 from the API keeps its own error code.</returns>
    public Task<Result<TValue>> PostAnonymousAsync<TValue>(string path, object body, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, path, body, ReadValueAsync<TValue>, Result<TValue>.Failure, cancellationToken, withCallerToken: false);

    private async Task<TResult> SendAsync<TResult>(
        HttpMethod method,
        string path,
        object? body,
        Func<HttpResponseMessage, CancellationToken, Task<TResult>> read,
        Func<string, TResult> fail,
        CancellationToken cancellationToken,
        bool withCallerToken = true)
        where TResult : Result
    {
        using var request = new HttpRequestMessage(method, path);
        if (withCallerToken && !string.IsNullOrEmpty(_staff?.Token))
        {
            request.Headers.TryAddWithoutValidation(ApiAuthentication.StaffTokenHeader, _staff.Token);
        }
        else if (withCallerToken && !string.IsNullOrEmpty(_guest?.Token))
        {
            // The guest token goes only where no staff token does (MIL-018): one call, one caller.
            request.Headers.TryAddWithoutValidation(ApiAuthentication.GuestTokenHeader, _guest.Token);
        }

        if (!string.IsNullOrEmpty(_staff?.ClientAddress))
        {
            request.Headers.TryAddWithoutValidation(ApiAuthentication.ClientAddressHeader, _staff.ClientAddress);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: ApiJson.Options);
        }

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                return ServiceUnavailableResult(await read(response, cancellationToken).ConfigureAwait(false), fail);
            }

            if ((int)response.StatusCode >= 500)
            {
                return fail(ErrorCodes.Api.Unavailable);
            }

            var answer = await read(response, cancellationToken).ConfigureAwait(false);
            return response.StatusCode == HttpStatusCode.Unauthorized ? UnauthorizedResult(answer, fail) : answer;
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

    // A 401 is the service token that was refused, unless the API says it is the staff token or the sign-in (ADR-0005),
    // or the guest token or the guest sign-in (MIL-018).
    private static TResult UnauthorizedResult<TResult>(TResult answer, Func<string, TResult> fail)
        where TResult : Result
    {
        var code = answer.FirstError?.Code;
        return code is not null
            && (code == ErrorCodes.Api.StaffUnauthorized
                || code == ErrorCodes.Api.GuestUnauthorized
                || code.StartsWith("staff.", StringComparison.Ordinal))
            ? answer
            : fail(ErrorCodes.Api.Unauthorized);
    }

    // A 503 is the API, or a proxy in front of it, that is down, unless the API says that the mail of a booking request
    // could not go: the relay did not take it, or the fuse of the mails of a day held it back (MIL-088, ADR-0053). The page
    // then tells the guest what to do, which "unavailable" would not. The page of a proxy is no envelope, so it reads as
    // unavailable.
    private static TResult ServiceUnavailableResult<TResult>(TResult answer, Func<string, TResult> fail)
        where TResult : Result
    {
        string? code = answer.FirstError?.Code;
        return code is ErrorCodes.Booking.MailNotSent or ErrorCodes.Booking.MailPaused
            ? answer
            : fail(ErrorCodes.Api.Unavailable);
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

    private static async Task<Result<TValue?>> ReadOptionalValueAsync<TValue>(HttpResponseMessage response, CancellationToken cancellationToken)
        where TValue : class
    {
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return Result<TValue?>.Success(null);
        }

        Result<TValue> read = await ReadValueAsync<TValue>(response, cancellationToken).ConfigureAwait(false);

        return read.IsSuccess ? Result<TValue?>.Success(read.Value) : Result<TValue?>.Failure(read.Errors);
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
