namespace NFHotel.Application.Dto.Benchmark;

/// <summary>The states of the download of a model that the benchmark service reports.</summary>
public static class ModelPullStates
{
    /// <summary>The download is waiting.</summary>
    public const string Queued = "queued";

    /// <summary>The download is running.</summary>
    public const string Pulling = "pulling";

    /// <summary>The model is installed.</summary>
    public const string Done = "done";

    /// <summary>The download ended with an error.</summary>
    public const string Failed = "failed";
}
