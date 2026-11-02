using Microsoft.Extensions.DependencyInjection;

using NFHotel.Api.Startup;

namespace NFHotel.Api.Tests;

/// <summary>
/// The command line of the seed-market-data command (MIL-054 task 8). The file is checked before the database is opened, so
/// these tests need no database; the writing itself is tested with the seeder in the infrastructure tests.
/// </summary>
public sealed class SeedMarketDataCommandTests : IDisposable
{
    private readonly string _file = Path.GetTempFileName();

    public void Dispose() => File.Delete(_file);

    private static Task<int> RunAsync(params string[] args)
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        return ApiStartup.RunSeedMarketDataAsync(services, args, TestContext.Current.CancellationToken);
    }

    /// <summary>Positive: only the exact command name is the seed command.</summary>
    [Theory]
    [InlineData("seed-market-data", true)]
    [InlineData("Seed-Market-Data", false)]
    [InlineData("seed", false)]
    public void IsSeedMarketData_Command_MatchesOnlyTheExactName(string command, bool expected) =>
        Assert.Equal(expected, ApiStartup.IsSeedMarketData([command]));

    /// <summary>Negative and boundary: a missing file, an unknown option or a missing or invalid date is a wrong command line.</summary>
    [Fact]
    public async Task Run_WrongCommandLine_ReturnsTwo()
    {
        Assert.Equal(2, await RunAsync(ApiStartup.SeedMarketDataCommand));
        Assert.Equal(2, await RunAsync(ApiStartup.SeedMarketDataCommand, Path.Combine(Path.GetTempPath(), "no-such-file.csv")));
        Assert.Equal(2, await RunAsync(ApiStartup.SeedMarketDataCommand, _file, "--unknown"));
        Assert.Equal(2, await RunAsync(ApiStartup.SeedMarketDataCommand, _file, ApiStartup.ThroughOption));
        Assert.Equal(2, await RunAsync(ApiStartup.SeedMarketDataCommand, _file, ApiStartup.ThroughOption, "01-03-2026"));
    }

    /// <summary>Error handling: a file that is empty or has the wrong header cannot be used, and nothing is written.</summary>
    [Fact]
    public async Task Run_UnreadableFile_ReturnsTwo()
    {
        Assert.Equal(2, await RunAsync(ApiStartup.SeedMarketDataCommand, _file, ApiStartup.ReplaceOption, ApiStartup.ThroughOption, "2026-03-01"));

        await File.WriteAllTextAsync(_file, "not,the,header\n1,2,3\n", TestContext.Current.CancellationToken);
        Assert.Equal(2, await RunAsync(ApiStartup.SeedMarketDataCommand, _file));
    }
}
