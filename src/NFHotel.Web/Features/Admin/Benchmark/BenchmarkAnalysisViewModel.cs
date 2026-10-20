using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.UseCases.Benchmark;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Admin.Benchmark;

/// <summary>
/// State of the analysis view of one open run (the view of the notebook of the benchmark repository): the workload that
/// is looked at and the answers that are compared side by side. The answers are read one by one, because they are long.
/// The page holds only markup and binding.
/// </summary>
public sealed class BenchmarkAnalysisViewModel
{
    /// <summary>How many answers can be compared at the same time.</summary>
    public const int MaxCompared = 3;

    private readonly IUseCaseRunner<IBenchmarkService> _benchmark;
    private readonly List<int> _compared = [];
    private readonly Dictionary<int, BenchmarkOutputDto> _outputs = [];

    /// <summary>Creates the view model.</summary>
    /// <param name="benchmark">Runs the use cases with the staff token of the signed-in user.</param>
    public BenchmarkAnalysisViewModel(IUseCaseRunner<IBenchmarkService> benchmark)
    {
        ArgumentNullException.ThrowIfNull(benchmark);

        _benchmark = benchmark;
    }

    /// <summary>The workload that the performance view looks at.</summary>
    public BenchmarkWorkload Workload { get; set; } = BenchmarkWorkload.Hotel;

    /// <summary>The run that the state belongs to, or <c>null</c> before a run is shown.</summary>
    public Guid? RunId { get; private set; }

    /// <summary>The keys of the answers that are compared, oldest choice first.</summary>
    public IReadOnlyList<int> Compared => _compared;

    /// <summary>The answers that have been read, by the key of the request.</summary>
    public IReadOnlyDictionary<int, BenchmarkOutputDto> Outputs => _outputs;

    /// <summary>The texts of the last refusal, empty when the last call succeeded.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = [];

    /// <summary>True while an answer is being read.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>Starts the state for a run; the compared answers of another run are forgotten.</summary>
    /// <param name="runId">The run that is shown.</param>
    public void ShowRun(Guid runId)
    {
        if (RunId == runId)
        {
            return;
        }

        RunId = runId;
        _compared.Clear();
        _outputs.Clear();
        Errors = [];
    }

    /// <summary>Tells whether the answer of a request is one of those that are compared.</summary>
    /// <param name="benchmarkResultId">The key of the request.</param>
    /// <returns>True when it is compared.</returns>
    public bool IsCompared(int benchmarkResultId) => _compared.Contains(benchmarkResultId);

    /// <summary>
    /// Adds the answer of a request to the comparison, or takes it out when it is in. At most <see cref="MaxCompared"/>
    /// answers are compared: the oldest choice leaves when a new one comes.
    /// </summary>
    /// <param name="benchmarkResultId">The key of the request.</param>
    /// <param name="cancellationToken">Cancels the call when the page is closed.</param>
    /// <returns>A task that ends when the answer is read.</returns>
    public async Task ToggleCompareAsync(int benchmarkResultId, CancellationToken cancellationToken = default)
    {
        if (_compared.Remove(benchmarkResultId))
        {
            return;
        }

        if (!_outputs.ContainsKey(benchmarkResultId))
        {
            IsBusy = true;
            try
            {
                var result = await _benchmark
                    .RunAsync((service, token) => service.GetOutputAsync(benchmarkResultId, token), cancellationToken)
                    .ConfigureAwait(false);

                if (result.IsFailure)
                {
                    Errors = ErrorMessages.Describe(result);
                    return;
                }

                _outputs[benchmarkResultId] = result.Value;
            }
            finally
            {
                IsBusy = false;
            }
        }

        Errors = [];
        _compared.Add(benchmarkResultId);
        while (_compared.Count > MaxCompared)
        {
            _compared.RemoveAt(0);
        }
    }
}
