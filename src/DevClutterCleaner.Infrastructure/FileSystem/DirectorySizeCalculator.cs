namespace DevClutterCleaner.Infrastructure.FileSystem;

public sealed class DirectorySizeCalculator : IDirectorySizeCalculator
{
    public long CalculateSize(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return 0;
        }

        return CalculateSizeSafe(new DirectoryInfo(path));
    }

    private static long CalculateSizeSafe(DirectoryInfo directory)
    {
        long total = 0;

        foreach (FileInfo file in EnumerateFilesSafe(directory))
        {
            try
            {
                total += file.Length;
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        foreach (DirectoryInfo childDirectory in EnumerateDirectoriesSafe(directory))
        {
            total += CalculateSizeSafe(childDirectory);
        }

        return total;
    }

    private static IEnumerable<FileInfo> EnumerateFilesSafe(DirectoryInfo directory)
    {
        try
        {
            return directory.EnumerateFiles();
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static IEnumerable<DirectoryInfo> EnumerateDirectoriesSafe(DirectoryInfo directory)
    {
        try
        {
            return directory.EnumerateDirectories();
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }
}
