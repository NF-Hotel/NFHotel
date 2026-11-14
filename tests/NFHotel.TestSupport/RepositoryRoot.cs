namespace NFHotel.TestSupport;

/// <summary>Finds the repository root from the test output folder, for tests that read source files.</summary>
public static class RepositoryRoot
{
    /// <summary>Finds the folder that holds <c>NFHotel.sln</c>, above the test output folder.</summary>
    /// <returns>The full path of the repository root.</returns>
    public static string Find()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Join(directory.FullName, "NFHotel.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("NFHotel.sln was not found above the test output folder.");
    }
}
