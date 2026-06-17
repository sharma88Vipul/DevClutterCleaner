namespace DevClutterCleaner.Infrastructure.FileSystem;

public sealed class DirectorySizeCalculator : IDirectorySizeCalculator
{
    public long CalculateSize(
        string path,
        CancellationToken cancellationToken,
        IProgress<string>? progress = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return 0;
        }

        return CalculateSizeSafe(new DirectoryInfo(path), cancellationToken, progress, new TraversalProgressState());
    }

    private static long CalculateSizeSafe(
        DirectoryInfo directory,
        CancellationToken cancellationToken,
        IProgress<string>? progress,
        TraversalProgressState progressState)
    {
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(directory.FullName);

        long total = 0;

        foreach (FileInfo file in EnumerateFilesSafe(directory))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (progressState.ShouldReportFile())
                {
                    progress?.Report(file.FullName);
                }

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
            total += CalculateSizeSafe(childDirectory, cancellationToken, progress, progressState);
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

    private sealed class TraversalProgressState
    {
        private int _fileCount;

        public bool ShouldReportFile()
        {
            _fileCount++;
            return _fileCount is 1 || _fileCount % 25 is 0;
        }
    }
}
