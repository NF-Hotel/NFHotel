using Microsoft.AspNetCore.Mvc;

using NFHotel.Api.Contracts;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;

namespace NFHotel.Api.Controllers;

/// <summary>Turns the result of a use case into the response of the API: a status and the envelope.</summary>
internal static class ResultResponses
{
    private static readonly string[] ConflictEndings =
    [
        ".concurrency_conflict",
        ".room_not_available",
        ".room_number_already_exists",
        ".has_bookings",
    ];

    /// <summary>Maps a result that carries a value.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="controller">The controller that answers.</param>
    /// <param name="result">The result of the use case.</param>
    /// <returns>200 with the value, or the failure status with the error codes.</returns>
    public static ObjectResult ToResponse<TValue>(this ControllerBase controller, Result<TValue> result)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess
            ? controller.Ok(new ApiResponse<TValue>(result.Value, []))
            : controller.StatusCode(StatusFor(result), new ApiResponse<TValue>(default, Codes(result)));
    }

    /// <summary>Maps a result without a value.</summary>
    /// <param name="controller">The controller that answers.</param>
    /// <param name="result">The result of the use case.</param>
    /// <returns>200, or the failure status with the error codes.</returns>
    public static ObjectResult ToResponse(this ControllerBase controller, Result result)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess
            ? controller.Ok(new ApiResponse([]))
            : controller.StatusCode(StatusFor(result), new ApiResponse(Codes(result)));
    }

    private static string[] Codes(Result result) => result.Errors.Select(error => error.Code).ToArray();

    private static int StatusFor(Result result)
    {
        var codes = Codes(result);
        if (codes.Contains(ErrorCodes.Staff.TooManyAttempts))
        {
            return StatusCodes.Status429TooManyRequests;
        }

        if (codes.Contains(ErrorCodes.Staff.InvalidCredentials))
        {
            return StatusCodes.Status401Unauthorized;
        }

        if (codes.Any(code => code.EndsWith(".not_found", StringComparison.Ordinal)))
        {
            return StatusCodes.Status404NotFound;
        }

        return codes.Any(code => ConflictEndings.Any(ending => code.EndsWith(ending, StringComparison.Ordinal)))
            ? StatusCodes.Status409Conflict
            : StatusCodes.Status422UnprocessableEntity;
    }
}