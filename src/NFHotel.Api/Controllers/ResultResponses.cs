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
        ".overlap",
        ".room_not_available",
        ".room_type_not_available",
        ".room_number_already_exists",
        ".has_bookings",
        ".already_running",
        ".user_name_taken",
        ".last_manager",
        ".cannot_disable_self",
        ".cannot_reset_self",
        ".verification_required",
    ];

    // The relay of the mail is a service of its own: a mail that it did not take, and the fuse of the mails a day, are
    // a service that is not there for now (MIL-088, ADR-0053 decisions 5 and 6).
    private static readonly string[] UnavailableEndings =
    [
        "_runner.unavailable",
        "_runner.not_configured",
        ".mail_not_sent",
        ".mail_paused",
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

    /// <summary>
    /// Maps a result of work that goes on after the answer: a booking request whose mail is on its way (MIL-088,
    /// ADR-0052 decision 1).
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="controller">The controller that answers.</param>
    /// <param name="result">The result of the use case.</param>
    /// <returns>202 with the value, or the failure status with the error codes.</returns>
    public static ObjectResult ToAcceptedResponse<TValue>(this ControllerBase controller, Result<TValue> result)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess
            ? controller.StatusCode(StatusCodes.Status202Accepted, new ApiResponse<TValue>(result.Value, []))
            : controller.ToResponse(result);
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
        if (codes.Contains(ErrorCodes.Staff.TooManyAttempts)
            || codes.Contains(ErrorCodes.Staff.TooManyRequests)
            || codes.Contains(ErrorCodes.Booking.TooManyLookups)
            || codes.Contains(ErrorCodes.Booking.TooManyRequests)
            || codes.Contains(ErrorCodes.Booking.TooManyMails))
        {
            return StatusCodes.Status429TooManyRequests;
        }

        if (codes.Contains(ErrorCodes.Staff.InvalidCredentials)
            || codes.Contains(ErrorCodes.Api.StaffUnauthorized)
            || codes.Contains(ErrorCodes.Api.GuestUnauthorized))
        {
            return StatusCodes.Status401Unauthorized;
        }

        // A use case refuses a caller whose role lacks the permission, and a guest whose booking it is not (MIL-018).
        if (codes.Contains(ErrorCodes.Api.Forbidden))
        {
            return StatusCodes.Status403Forbidden;
        }

        if (codes.Any(code => code.EndsWith(".not_found", StringComparison.Ordinal)
            || code == ErrorCodes.MarketAnalysisRunner.RunNotFound))
        {
            return StatusCodes.Status404NotFound;
        }

        if (codes.Any(code => UnavailableEndings.Any(ending => code.EndsWith(ending, StringComparison.Ordinal))))
        {
            return StatusCodes.Status503ServiceUnavailable;
        }

        return codes.Any(code => ConflictEndings.Any(ending => code.EndsWith(ending, StringComparison.Ordinal)))
            ? StatusCodes.Status409Conflict
            : StatusCodes.Status422UnprocessableEntity;
    }
}
