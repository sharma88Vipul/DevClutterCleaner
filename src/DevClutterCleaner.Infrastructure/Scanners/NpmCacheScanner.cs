using DevClutterCleaner.Application.Abstractions;
using DevClutterCleaner.Domain;
using DevClutterCleaner.Infrastructure.FileSystem;

namespace DevClutterCleaner.Infrastructure.Scanners;

public sealed class NpmCacheScanner : ICacheScanner
{
    public const string ScannerId = "npm-cache";

    private readonly IDirectorySizeCalculator _directorySizeCalculator;
    private readonly Func<string> _pathProvider;

    public NpmCacheScanner(IDirectorySizeCalculator directorySizeCalculator)
        : this(directorySizeCalculator, GetDefaultNpmCachePath)
    {
    }

    public NpmCacheScanner(IDirectorySizeCalculator directorySizeCalculator, Func<string> pathProvider)
    {
        _directorySizeCalculator = directorySizeCalculator;
        _pathProvider = pathProvider;
    }

    public string Id => ScannerId;

    public string TargetDisplayName => "npm Cache";

    public CacheCategory TargetType => CacheCategory.PackageManager;

    public string GetTargetPath() => _pathProvider();

    public Task<ScanResult> ScanAsync(
        CancellationToken cancellationToken,
        IProgress<ScanProgress>? progress = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string path = GetTargetPath();
        progress?.Report(new ScanProgress(TargetDisplayName, path));
        CacheTarget target = new(
            ScannerId,
            TargetDisplayName,
            TargetType,
            path,
            IsSafeToCleanByDefault: true);

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

    private static string GetDefaultNpmCachePath()
    {
        string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localApplicationData, "npm-cache");
    }
}
