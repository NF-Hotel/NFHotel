namespace NFHotel.Application.Dto.Benchmark;

/// <summary>The states of the delivery of a result to this application, as the benchmark service names them.</summary>
public static class BenchmarkDeliveryStates
{
    /// <summary>The run has not finished, so there is nothing to deliver.</summary>
    public const string NotReady = "not_ready";

    /// <summary>The result waits for this application to accept it.</summary>
    public const string Pending = "pending";

    /// <summary>This application has the result; it can be read in the list of runs.</summary>
    public const string Delivered = "delivered";

    /// <summary>The delivery was given up; the result is kept by the benchmark service.</summary>
    public const string Failed = "failed";
}