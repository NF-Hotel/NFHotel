namespace NFHotel.Api.Contracts;

/// <summary>The routes of the API, in one place so that the controllers and the client cannot drift apart.</summary>
public static class ApiRoutes
{
    /// <summary>The path of the health endpoint, open without a token.</summary>
    public const string Health = "health";

    /// <summary>The route prefix of the booking endpoints.</summary>
    public const string Bookings = "api/bookings";

    /// <summary>The route for public guest booking creation.</summary>
    public const string PublicBooking = "public";

    /// <summary>The route prefix of the guest endpoints.</summary>
    public const string Guests = "api/guests";

    /// <summary>The route prefix of the room endpoints.</summary>
    public const string Rooms = "api/rooms";

    /// <summary>The route prefix of the default AI models (ADR-0014).</summary>
    public const string AiDefaults = "api/ai-defaults";

    /// <summary>The default of one purpose below <see cref="AiDefaults"/>.</summary>
    public const string AiDefaultByPurpose = "{purpose}";

    /// <summary>The route prefix of the benchmark results (ADR-0013).</summary>
    public const string Benchmark = "api/benchmark";

    /// <summary>The delivery of a result below <see cref="Benchmark"/>.</summary>
    public const string BenchmarkResults = "results";

    /// <summary>The list of runs below <see cref="Benchmark"/>.</summary>
    public const string BenchmarkRuns = "runs";

    /// <summary>One run below <see cref="Benchmark"/>, by the run id of the contract.</summary>
    public const string BenchmarkRunById = "runs/{runId:guid}";

    /// <summary>The records of the started runs below <see cref="Benchmark"/> (ADR-0031).</summary>
    public const string BenchmarkRequests = "requests";

    /// <summary>The state of one started run below <see cref="Benchmark"/>, by the run id of the service.</summary>
    public const string BenchmarkRequestState = "requests/{runId:guid}/state";

    /// <summary>The answer text of one request below <see cref="Benchmark"/>.</summary>
    public const string BenchmarkOutput = "results/{id:int}/output";

    /// <summary>The route prefix of the market analysis (MIL-026).</summary>
    public const string MarketAnalysis = "api/market-analysis";

    /// <summary>The latest stored report below <see cref="MarketAnalysis"/>.</summary>
    public const string MarketAnalysisLatest = "latest";

    /// <summary>The runs below <see cref="MarketAnalysis"/>: a start posts here.</summary>
    public const string MarketAnalysisRuns = "runs";

    /// <summary>One run below <see cref="MarketAnalysis"/>, by the run id of the data analysis service.</summary>
    public const string MarketAnalysisRunById = "runs/{runId:guid}";

    /// <summary>The route prefix of the staff sign-in (ADR-0005).</summary>
    public const string Auth = "api/auth";

    /// <summary>The sign-in below <see cref="Auth"/>.</summary>
    public const string Login = "login";

    /// <summary>The route prefix of the staff accounts (ADR-0017).</summary>
    public const string Staff = "api/staff";

    /// <summary>The change of the own password below <see cref="Staff"/>.</summary>
    public const string StaffMyPassword = "me/password";

    /// <summary>Disabling an account below <see cref="Staff"/>.</summary>
    public const string StaffDisable = "{id:int}/disable";

    /// <summary>Enabling an account below <see cref="Staff"/>.</summary>
    public const string StaffEnable = "{id:int}/enable";

    /// <summary>The reset of the password of an account below <see cref="Staff"/>.</summary>
    public const string StaffResetPassword = "{id:int}/password";

    /// <summary>The audit trail below <see cref="Staff"/>; the query parameters <c>page</c> and <c>pageSize</c> select the page.</summary>
    public const string StaffAudit = "audit";

    /// <summary>A resource by key: <c>{id}</c> after the prefix.</summary>
    public const string ById = "{id:int}";

    /// <summary>The overview of bookings of a period (a body, because the query has several parts).</summary>
    public const string Overview = "overview";

    /// <summary>The free rooms of a period.</summary>
    public const string AvailableRooms = "available-rooms";

    /// <summary>Moving a booking to other dates or another room.</summary>
    public const string Reschedule = "reschedule";

    /// <summary>Searching with a filter in the body.</summary>
    public const string Search = "search";

    /// <summary>The values of the room filter.</summary>
    public const string FilterOptions = "filter-options";

    /// <summary>The path after the key for each action on a booking.</summary>
    public static class BookingAction
    {
        /// <summary>Confirm.</summary>
        public const string Confirm = "{id:int}/confirm";

        /// <summary>Check in.</summary>
        public const string CheckIn = "{id:int}/check-in";

        /// <summary>Check out.</summary>
        public const string CheckOut = "{id:int}/check-out";

        /// <summary>Cancel.</summary>
        public const string Cancel = "{id:int}/cancel";
    }

    /// <summary>The path after the key for each action on a room.</summary>
    public static class RoomAction
    {
        /// <summary>Take out of service.</summary>
        public const string TakeOutOfService = "{id:int}/take-out-of-service";

        /// <summary>Send to maintenance.</summary>
        public const string SendToMaintenance = "{id:int}/send-to-maintenance";

        /// <summary>Return to service.</summary>
        public const string ReturnToService = "{id:int}/return-to-service";

        /// <summary>Mark for daily cleaning.</summary>
        public const string MarkDailyCleaningDue = "{id:int}/mark-daily-cleaning-due";

        /// <summary>Mark for departure cleaning.</summary>
        public const string MarkDepartureCleaningDue = "{id:int}/mark-departure-cleaning-due";

        /// <summary>Start cleaning.</summary>
        public const string StartCleaning = "{id:int}/start-cleaning";

        /// <summary>Complete cleaning.</summary>
        public const string CompleteCleaning = "{id:int}/complete-cleaning";

        /// <summary>Report that service is needed.</summary>
        public const string ReportServiceNeeded = "{id:int}/report-service-needed";

        /// <summary>Start service.</summary>
        public const string StartService = "{id:int}/start-service";

        /// <summary>Complete service; the query parameter <c>requiresCleaning</c> says whether cleaning follows.</summary>
        public const string CompleteService = "{id:int}/complete-service";
    }
}
