using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;

namespace NFHotel.Web.Common;

/// <summary>
/// Translates the error codes of the Application layer to Danish user texts.
/// </summary>
/// <remarks>
/// A-09: the code is the contract, the text is the responsibility of the UI. Therefore the translation exists in exactly
/// one place — here — and not spread out in the components. A later API maps the same codes
/// to HTTP statuses without inheriting a single Danish string.
/// <para>
/// Unknown codes (and any unexpected exception) become <see cref="Unexpected"/>. We never
/// show a raw error code or an exception text to the user, cf. Conventions.md's
/// "Show the user neutral error messages".
/// </para>
/// </remarks>
public static class ErrorMessages
{
    /// <summary>
    /// The neutral message shown when something went wrong without a known error code.
    /// </summary>
    public const string Unexpected = "Something went wrong. Please try again.";

    private static readonly IReadOnlyDictionary<string, string> Translations =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // ----------------------------------------------------------
            // WEB
            // ----------------------------------------------------------
            [WebErrorCodes.Unexpected] = Unexpected,

            // ----------------------------------------------------------
            // BOOKING
            // ----------------------------------------------------------
            [ErrorCodes.Booking.NotFound] = "We couldn't find that booking.",
            [ErrorCodes.Booking.StartDateRequired] = "Enter a check-in date.",
            [ErrorCodes.Booking.EndDateRequired] = "Enter a check-out date.",
            [ErrorCodes.Booking.EndNotAfterStart] = "The check-out date must be after the check-in date.",
            [ErrorCodes.Booking.StartDateInPast] = "The check-in date can't be in the past.",
            [ErrorCodes.Booking.RoomRequired] = "Select a room.",
            [ErrorCodes.Booking.GuestRequired] = "Select a guest.",
            [ErrorCodes.Booking.CheckOutBeforeCheckIn] = "The guest can't check out before they check in.",
            [ErrorCodes.Booking.CheckOutDateBeforeStartDate] = "The check-out date can't be before the check-in date.",
            [ErrorCodes.Booking.CheckOutDateInFuture] = "The check-out date can't be in the future.",
            [ErrorCodes.Booking.ConfirmNotAllowed] = "This booking can't be confirmed in its current state.",
            [ErrorCodes.Booking.CheckInNotAllowed] = "The guest can't be checked in while the booking is in its current state.",
            [ErrorCodes.Booking.CheckOutNotAllowed] = "The guest can't be checked out while the booking is in its current state.",
            [ErrorCodes.Booking.CancelNotAllowed] = "This booking can't be cancelled in its current state.",
            [ErrorCodes.Booking.RescheduleNotAllowed] = "This booking can't be moved in its current state.",
            [ErrorCodes.Booking.RoomNotAvailable] = "That room is already taken for the selected dates.",
            [ErrorCodes.Booking.RoomNotBookable] = "That room can't be booked right now.",
            [ErrorCodes.Booking.OverviewPeriodInvalid] = "The end of the period can't be before its start.",
            [ErrorCodes.Booking.GuestSelectionInvalid] = "Pick an existing guest or create a new one — not both.",
            [ErrorCodes.Booking.ConcurrencyConflict] = "Someone else changed this booking. Reload the page and start over.",

            // ----------------------------------------------------------
            // ROOM
            // ----------------------------------------------------------
            [ErrorCodes.Room.NotFound] = "We couldn't find that room.",
            [ErrorCodes.Room.RoomNumberRequired] = "Enter a room number.",
            [ErrorCodes.Room.RoomNumberTooLong] = "That room number is too long.",
            [ErrorCodes.Room.FloorInvalid] = "The floor must be a positive number.",
            [ErrorCodes.Room.SizeInvalid] = "Pick a valid room size.",
            [ErrorCodes.Room.CapacityInvalid] = "The capacity must be a positive number.",
            [ErrorCodes.Room.RoomNumberAlreadyExists] = "A room with that number already exists.",
            [ErrorCodes.Room.HasBookings] = "This room can't be deleted because it still has bookings on it.",
            [ErrorCodes.Room.MarkDailyCleaningDueNotAllowed] = "This room can't be marked for daily cleaning in its current state.",
            [ErrorCodes.Room.MarkDepartureCleaningDueNotAllowed] = "This room can't be marked for departure cleaning in its current state.",
            [ErrorCodes.Room.StartCleaningNotAllowed] = "Cleaning can't be started while the room is in its current state.",
            [ErrorCodes.Room.CompleteCleaningNotAllowed] = "Cleaning can't be completed while the room is in its current state.",
            [ErrorCodes.Room.ReportServiceNeededNotAllowed] = "Service can't be reported while the room is in its current state.",
            [ErrorCodes.Room.StartServiceNotAllowed] = "Service can't be started while the room is in its current state.",
            [ErrorCodes.Room.CompleteServiceNotAllowed] = "Service can't be completed while the room is in its current state.",
            [ErrorCodes.Room.ConcurrencyConflict] = "Someone else changed this room. Reload the page and start over.",

            // ----------------------------------------------------------
            // GUEST
            // ----------------------------------------------------------
            [ErrorCodes.Guest.NotFound] = "We couldn't find that guest.",
            [ErrorCodes.Guest.FirstNameRequired] = "Enter a first name.",
            [ErrorCodes.Guest.FirstNameTooLong] = "That first name is too long.",
            [ErrorCodes.Guest.LastNameRequired] = "Enter a last name.",
            [ErrorCodes.Guest.LastNameTooLong] = "That last name is too long.",
            [ErrorCodes.Guest.EmailRequired] = "Enter an email address.",
            [ErrorCodes.Guest.EmailInvalid] = "The email address must contain both '@' and '.'.",
            [ErrorCodes.Guest.EmailTooLong] = "That email address is too long.",
            [ErrorCodes.Guest.PhoneNumberRequired] = "Enter a phone number.",
            [ErrorCodes.Guest.PhoneNumberTooLong] = "That phone number is too long.",
            [ErrorCodes.Guest.CountryRequired] = "Enter a country.",
            [ErrorCodes.Guest.CountryTooLong] = "That country name is too long.",
            [ErrorCodes.Guest.PassportNumberTooLong] = "That passport number is too long.",

            // ----------------------------------------------------------
            // HELLIGDAGE (analysetjenesten)
            // ----------------------------------------------------------
            // Configuration and key errors are an operations fault: the user cannot fix them, so
            // the text is neutral. The detail is in the log (HolidayApiClient).
            [ErrorCodes.Api.Unavailable] = "We couldn't reach the hotel system. Please try again in a moment.",
            [ErrorCodes.Api.Unauthorized] = "The hotel system refused this application. Please tell the administrator.",
            [ErrorCodes.Api.UnexpectedResponse] = "The hotel system answered something we couldn't use. Please try again.",
            [ErrorCodes.Api.StaffUnauthorized] = "Your session has ended. Sign in again.",
            [ErrorCodes.Api.Forbidden] = "Your role isn't allowed to do that.",
            [ErrorCodes.Staff.InvalidCredentials] = "The user name or the password is wrong.",
            [ErrorCodes.Staff.TooManyAttempts] = "Too many failed attempts. Wait fifteen minutes and try again.",
            [AiDefaultErrors.PurposeInvalid] = "The default AI model can be set for the website or the market analysis.",
            [AiDefaultErrors.ModelInvalid] = "Enter a model name of at most 100 characters.",
            [AiDefaultErrors.SetByInvalid] = "The staff member could not be named. Please sign in again.",
            [RoomTradeErrors.NotFound] = "That room trade was not found.",
            [RoomTradeErrors.NotYours] = "Only the housekeeper the trade is for, or who offered it, can do that.",
            [RoomTradeErrors.NotOpen] = "That room trade has already been answered.",
            [RoomTradeErrors.RoomsChanged] = "A room of the trade changed hands meanwhile, so the trade was called off.",
            [RoomTradeErrors.NoRooms] = "Choose at least one room to give.",
            [RoomTradeErrors.DuplicateRoom] = "A room is chosen twice.",
            [RoomTradeErrors.SameStaff] = "Choose another housekeeper.",
            [RoomTradeErrors.NotHousekeeper] = "Rooms can only be traded with an active housekeeper.",
            [RoomTradeErrors.RoomNotYours] = "You can only give rooms that are assigned to you.",
            [RoomTradeErrors.RoomNotTheirs] = "You can only ask for rooms that are assigned to the colleague.",
            [RoomTradeErrors.KeyInvalid] = "That room or housekeeper was not found.",
            [RoomTradeErrors.AssigneeInvalid] = "Rooms can only be assigned to an active housekeeper.",
            [ShiftErrors.NotFound] = "That shift was not found.",
            [ShiftErrors.EndBeforeStart] = "The shift must end after it starts.",
            [ShiftErrors.TooLong] = "A shift can be at most 16 hours.",
            [ShiftErrors.RoleInvalid] = "Choose a role for the shift.",
            [ShiftErrors.AssigneeInvalid] = "The shift can only go to an active member of staff with its role.",
            [ShiftErrors.PeriodInvalid] = "Choose a period of at most 62 days.",
            [ShiftErrors.Overlap] = "That member of staff already has a shift at that time.",
            [ShiftErrors.TradeNotFound] = "That shift trade was not found.",
            [ShiftErrors.TradeNotYours] = "Only the colleague the trade is for, or who offered it, can do that.",
            [ShiftErrors.TradeNotOpen] = "That shift trade has already been answered.",
            [ShiftErrors.TradeExpired] = "The shift starts within 24 hours, so it can no longer be traded.",
            [ShiftErrors.TradeAlreadyOffered] = "That shift is already offered in another trade.",
            [ShiftErrors.TradeWrongRole] = "Only staff with the role of the shift can take it.",
            [ShiftErrors.TradeOwnShift] = "Choose a shift of a colleague.",
            [ShiftErrors.TradeSwapInvalid] = "That shift cannot be swapped: it has nobody or another role.",
            [ShiftErrors.TradeShiftChanged] = "A shift of the trade changed hands meanwhile, so the trade was called off.",
            [RoomServiceNoteErrors.NotFound] = "We couldn't find that note. It may have been removed with its room.",
            [RoomServiceNoteErrors.RoomInvalid] = "We couldn't find that room.",
            [RoomServiceNoteErrors.TextRequired] = "Write what the room needs.",
            [RoomServiceNoteErrors.TextTooLong] = FormattableString.Invariant($"Keep the note to at most {RoomServiceNote.MaxTextLength:N0} characters."),
            [RoomServiceNoteErrors.WrittenByInvalid] = "The staff member could not be named. Please sign in again.",
            [RoomServiceNoteErrors.DoneByInvalid] = "The staff member could not be named. Please sign in again.",
            [RoomServiceNoteErrors.UndoWindowClosed] = "This note has moved to the room's history. Only a manager can reopen it now.",
            [RoomRateErrors.SizeInvalid] = "The room size must be Single, Double or Suite.",
            [RoomRateErrors.PriceInvalid] = FormattableString.Invariant($"Enter a nightly price above 0 and at most {RoomRate.MaxNightlyPrice:N0}."),
            [RoomRateErrors.ConcurrencyConflict] = "Someone else saved this rate at the same moment. Reload the page and try again.",
            [RoomRateErrors.SetByInvalid] = "The staff member could not be named. Please sign in again.",
            [PricingSettingErrors.SurchargeInvalid] = "Enter a surcharge from 0 to 100 percent.",
            [PricingSettingErrors.DiscountInvalid] = "Enter a discount from 0 to 100 percent.",
            [PricingSettingErrors.HighThresholdInvalid] = "Enter a high season threshold from 0 to 100 percent.",
            [PricingSettingErrors.LowThresholdInvalid] = "Enter a low season threshold from 0 to 100 percent.",
            [PricingSettingErrors.ThresholdsOrderInvalid] = "The low season threshold must be below the high season threshold.",
            [PricingSettingErrors.MaxChangeInvalid] = "Enter a largest change above 0 and at most 100 percent.",
            [PricingSettingErrors.MinimumPriceInvalid] = FormattableString.Invariant($"Enter a minimum nightly price from 0 to {PricingSetting.MaxMinimumPrice:N0}."),
            [PricingSettingErrors.DiscountSizesInvalid] = "Choose at least one room size for the discount.",
            [PricingSettingErrors.ConcurrencyConflict] = "Someone else saved the settings at the same moment. Reload the page and try again.",
            [PricingSettingErrors.SetByInvalid] = "The staff member could not be named. Please sign in again.",
            [PriceChangeErrors.SizeInvalid] = "The room size must be Single, Double or Suite.",
            [PriceChangeErrors.PercentageInvalid] = FormattableString.Invariant($"Enter a change that is not 0, from a discount of just under 100 percent to a surcharge of {PriceChange.MaxPercentage:0} percent."),
            [PriceChangeErrors.PeriodInvalid] = FormattableString.Invariant($"The last day must not be before the first day or before today, and the period can be at most {PriceChange.MaxPeriodDays} days."),
            [PriceChangeErrors.ApprovedByInvalid] = "The staff member could not be named. Please sign in again.",
            [PriceChangeErrors.RevertedByInvalid] = "The staff member could not be named. Please sign in again.",
            [PriceChangeErrors.AlreadyReverted] = "The price change is already reverted.",
            [PriceChangeErrors.PeriodEnded] = "The period of the price change has ended, so there is nothing left to revert.",
            [PriceChangeErrors.NotFound] = "The price change was not found.",
            [PriceChangeErrors.Overlap] = "Another price change of the same room size already counts on a day of this period. Shorten the period or revert the other change first.",
            [PriceSuggestionErrors.NotFound] = "The suggestion is no longer open. Reload the list to see the current suggestions.",
            [ErrorCodes.Benchmark.NotFound] = "The benchmark run was not found.",
            [BenchmarkRequestErrors.RunIdRequired] = "The run was started, but the hotel could not record it: it has no id.",
            [BenchmarkRequestErrors.ModelsInvalid] = "The run was started, but the hotel could not record its models.",
            [BenchmarkRequestErrors.TasksInvalid] = "The run was started, but the hotel could not record it: no analysis was chosen.",
            [BenchmarkRequestErrors.RunsInvalid] = "The run was started, but the hotel could not record its number of runs.",
            [BenchmarkRequestErrors.RequestedByInvalid] = "The run was started, but the hotel could not record who started it. Please sign in again.",
            [BenchmarkRequestErrors.StateInvalid] = "The benchmark service reported a state the hotel does not know.",
            [BenchmarkRequestErrors.StateChangeRefused] = "The run has already ended; its state is not changed again.",
            [BenchmarkRequestErrors.ErrorInvalid] = "The reason the run stopped is too long to keep.",
            [ErrorCodes.Benchmark.SchemaVersionUnsupported] = "The result uses a schema version that is not supported.",
            [ErrorCodes.Benchmark.BodyInvalid] = "The benchmark result is incomplete.",
            [ErrorCodes.BenchmarkRunner.NotConfigured] = "The benchmark service is not set up. Please tell the administrator.",
            [ErrorCodes.BenchmarkRunner.Unavailable] = "We couldn't reach the benchmark service. Please try again in a moment.",
            [ErrorCodes.BenchmarkRunner.Unauthorized] = "The benchmark service refused this application. Please tell the administrator.",
            [ErrorCodes.BenchmarkRunner.InvalidRequest] = "Choose one to four models, at least one analysis, and one to five runs.",
            [ErrorCodes.BenchmarkRunner.ModelNotInstalled] = "A chosen model is not installed on the benchmark service.",
            [ErrorCodes.BenchmarkRunner.QueueFull] = "A run is active and others are waiting. Try again in a few minutes.",
            [ErrorCodes.BenchmarkRunner.RunNotFound] = "The benchmark service was restarted and the run was stopped. It is shown as interrupted; start it again with the same choice.",
            [ErrorCodes.BenchmarkRunner.InvalidModelName] = "That is not the name of an Ollama model. Use letters, digits and . _ - / : only, for example phi4-mini or llama3.2:3b.",
            [ErrorCodes.BenchmarkRunner.ModelAlreadyInstalled] = "That model is already installed.",
            [ErrorCodes.BenchmarkRunner.PullActive] = "Another model is being added. Wait until it is done.",
            [ErrorCodes.BenchmarkRunner.PullNotFound] = "The benchmark service no longer knows this download. Look at the list of installed models.",
            [ErrorCodes.BenchmarkRunner.ModelNotFound] = "That model is not installed any more.",
            [ErrorCodes.BenchmarkRunner.ModelInUse] = "A queued or running benchmark uses this model. Delete it when the run is done.",
            [ErrorCodes.BenchmarkRunner.ModelServerError] = "The model server did not do it. Please try again in a moment.",
            [ErrorCodes.BenchmarkRunner.UnexpectedResponse] = "The benchmark service answered something we couldn't use. Please try again.",
            [ErrorCodes.MarketAnalysis.NotFound] = "No market analysis has been run yet.",
            [ErrorCodes.MarketAnalysis.SchemaVersionUnsupported] = "The market analysis uses a result format that is not supported.",
            [ErrorCodes.MarketAnalysis.BodyInvalid] = "The market analysis result is incomplete.",
            [ErrorCodes.MarketAnalysisRunner.NotConfigured] = "The market analysis service is not set up. Please tell the administrator.",
            [ErrorCodes.MarketAnalysisRunner.Unavailable] = "We couldn't reach the market analysis service, or it took too long. Please try again.",
            [ErrorCodes.MarketAnalysisRunner.Unauthorized] = "The market analysis service refused the call. Please tell the administrator.",
            [ErrorCodes.MarketAnalysisRunner.AlreadyRunning] = "An analysis is already running. Wait until it is done.",
            [ErrorCodes.MarketAnalysisRunner.InputRejected] = "The market analysis service refused the bookings. Please tell the administrator.",
            [ErrorCodes.MarketAnalysis.NoBookings] = "The hotel has no bookings to analyse in that period.",
            [ErrorCodes.MarketAnalysis.PeriodInvalid] = "The first date must not be after the last date.",
            [ErrorCodes.MarketAnalysisRunner.RunNotFound] = "The API no longer knows this run (it was restarted). The report below is the last stored one.",
            [ErrorCodes.MarketAnalysisRunner.RunFailed] = "The market analysis run failed in the service.",
            [ErrorCodes.MarketAnalysisRunner.UnexpectedResponse] = "The market analysis service answered something we couldn't use. Please try again.",
            [ErrorCodes.Staff.PasswordInvalid] = "The password must be between 12 and 256 characters.",
            [ErrorCodes.Staff.CurrentPasswordInvalid] = "The current password is wrong.",
            [ErrorCodes.Staff.PasswordSameAsUserName] = "The password must not be the same as the user name.",
            [ErrorCodes.Staff.PasswordSameAsCurrent] = "Choose a password that is different from the current one.",
            [ErrorCodes.Staff.PasswordTooCommon] = "That password is too common. Choose another one.",
            [ErrorCodes.Staff.NotFound] = "The account was not found.",
            [ErrorCodes.Staff.LastManager] = "At least one active Manager must remain, so this account cannot be disabled, deleted or given another role.",
            [ErrorCodes.Staff.CannotDisableSelf] = "You cannot disable your own account.",
            [ErrorCodes.Staff.CannotResetSelf] = "Use Change password to set your own password.",
            [ErrorCodes.Staff.PasswordChangeRequired] = "Choose a new password before you continue.",
            [ErrorCodes.Staff.TooManyRequests] = "Too many changes in a short time. Wait a minute and try again.",
            [ErrorCodes.Staff.UserNameTaken] = "That user name is already in use.",
            [ErrorCodes.Staff.UserNameRequired] = "Enter a user name.",
            [ErrorCodes.Staff.UserNameTooLong] = "The user name is too long.",
            [ErrorCodes.Staff.RoleInvalid] = "Select a role.",
            [ErrorCodes.Staff.FirstNameRequired] = "Enter a first name.",
            [ErrorCodes.Staff.FirstNameTooLong] = "The first name is too long.",
            [ErrorCodes.Staff.LastNameRequired] = "Enter a last name.",
            [ErrorCodes.Staff.LastNameTooLong] = "The last name is too long.",
            [ErrorCodes.Staff.EmailInvalid] = "Enter a valid e-mail address.",
            [ErrorCodes.Staff.EmailTooLong] = "The e-mail address is too long.",
            [ErrorCodes.Staff.EmailTaken] = "Another account already has that e-mail address.",
            [ErrorCodes.Staff.CannotChangeOwnRole] = "You cannot change your own role.",
            [ErrorCodes.Staff.CannotDeleteSelf] = "You cannot delete your own account.",
            [ErrorCodes.Staff.NameAlreadySet] = "The account already has a name. Ask a Manager to change it.",
            [ErrorCodes.Holidays.NotConfigured] = "Holiday data isn't available right now.",
            [ErrorCodes.Holidays.Unauthorized] = "Holiday data isn't available right now.",
            [ErrorCodes.Holidays.Unavailable] = "We couldn't reach the holiday service. Please try again later.",
            [ErrorCodes.Holidays.InvalidRequest] = "The selected years aren't valid.",
            [ErrorCodes.Holidays.UnexpectedResponse] = "Holiday data isn't available right now.",
            [ErrorCodes.Guest.HasBookings] = "This guest can't be deleted because they still have bookings.",
            [ErrorCodes.Guest.ConcurrencyConflict] = "Someone else changed this guest. Reload the page and start over."
        };

    /// <summary>
    /// Translates one error code to Danish.
    /// </summary>
    /// <param name="error">The error from the Application layer.</param>
    /// <returns>The Danish text, or <see cref="Unexpected"/> if the code is unknown.</returns>
    public static string Describe(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (Translations.TryGetValue(error.Code, out var text))
        {
            return text;
        }

        // A failed run carries the sanitised error_code of the service after a colon; it is shown, because it is a reason from
        // the service (for example NO_DATA) and not an exception text.
        var failedPrefix = ErrorCodes.MarketAnalysisRunner.RunFailed + ":";
        return error.Code.StartsWith(failedPrefix, StringComparison.Ordinal) && error.Code.Length > failedPrefix.Length
            ? $"The market analysis run failed in the service ({error.Code[failedPrefix.Length..]})."
            : Unexpected;
    }

    /// <summary>
    /// Translates all errors in a result to Danish texts.
    /// </summary>
    /// <param name="result">A failed result from the Application layer.</param>
    /// <returns>One text per error. Empty if the result succeeded.</returns>
    /// <remarks>
    /// Several errors at once is the intention, not a fault: BR-60 and BR-96 require that a form
    /// can show all validation errors at the same time.
    /// </remarks>
    public static IReadOnlyList<string> Describe(Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            return Array.Empty<string>();
        }

        var texts = new string[result.Errors.Count];

        for (var i = 0; i < result.Errors.Count; i++)
        {
            texts[i] = Describe(result.Errors[i]);
        }

        return texts;
    }
}
