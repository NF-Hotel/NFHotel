namespace NFHotel.Application.Tests.Architecture;

/// <summary>Finds the repository root from the test output folder, for tests that read source files.</summary>
internal static class RepositoryRoot
{
    public static string Find()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "NFHotel.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("NFHotel.sln was not found above the test output folder.");
    }
}
