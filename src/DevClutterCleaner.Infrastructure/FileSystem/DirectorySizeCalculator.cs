namespace DevClutterCleaner.Infrastructure.FileSystem;

public sealed class DirectorySizeCalculator : IDirectorySizeCalculator
{
    public long CalculateSize(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return 0;
        }

        return CalculateSizeSafe(new DirectoryInfo(path), cancellationToken);
    }

    private static long CalculateSizeSafe(DirectoryInfo directory, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        long total = 0;

        foreach (FileInfo file in EnumerateFilesSafe(directory))
        {
            cancellationToken.ThrowIfCancellationRequested();

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
            cancellationToken.ThrowIfCancellationRequested();
            total += CalculateSizeSafe(childDirectory, cancellationToken);
        }

        return total;
    }

    private static IReadOnlyList<FileInfo> EnumerateFilesSafe(DirectoryInfo directory)
    {
        try
        {
            return directory.EnumerateFiles().ToArray();
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

    private static IReadOnlyList<DirectoryInfo> EnumerateDirectoriesSafe(DirectoryInfo directory)
    {
        try
        {
            return directory.EnumerateDirectories().ToArray();
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
