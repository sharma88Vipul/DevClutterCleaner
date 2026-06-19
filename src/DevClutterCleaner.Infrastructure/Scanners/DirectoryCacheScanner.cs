using DevClutterCleaner.Application.Abstractions;
using DevClutterCleaner.Domain;
using DevClutterCleaner.Infrastructure.FileSystem;

namespace DevClutterCleaner.Infrastructure.Scanners;

public abstract class DirectoryCacheScanner : ICacheScanner
{
    private readonly IDirectorySizeCalculator _directorySizeCalculator;
    private readonly Func<string> _pathProvider;

    protected DirectoryCacheScanner(
        IDirectorySizeCalculator directorySizeCalculator,
        Func<string> pathProvider)
    {
        _directorySizeCalculator = directorySizeCalculator;
        _pathProvider = pathProvider;
    }

    public abstract string Id { get; }

    public abstract string TargetDisplayName { get; }

    public abstract CacheCategory TargetType { get; }

    protected abstract bool IsSafeToCleanByDefault { get; }

    public string GetTargetPath() => _pathProvider();

    public Task<ScanResult> ScanAsync(
        CancellationToken cancellationToken,
        IProgress<ScanProgress>? progress = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string path = GetTargetPath();
        progress?.Report(new ScanProgress(TargetDisplayName, path));
        CacheTarget target = new(
            Id,
            TargetDisplayName,
            TargetType,
            path,
            IsSafeToCleanByDefault);

        if (!Directory.Exists(path))
        {
            return Task.FromResult(new ScanResult(target, 0, Exists: false));
        }

        try
        {
            long size = _directorySizeCalculator.CalculateSize(
                path,
                cancellationToken,
                new Progress<string>(currentPath => progress?.Report(new ScanProgress(TargetDisplayName, currentPath))));
            return Task.FromResult(new ScanResult(target, size, Exists: true));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(new ScanResult(target, 0, Exists: true, ErrorMessage: ex.Message));
        }
    }
}
