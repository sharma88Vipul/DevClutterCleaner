using DevClutterCleaner.Application.Abstractions;
using DevClutterCleaner.Domain;
using DevClutterCleaner.Infrastructure.FileSystem;

namespace DevClutterCleaner.Infrastructure.Scanners;

public sealed class WindowsTempScanner : ICacheScanner
{
    public const string ScannerId = "windows-temp";

    private readonly IDirectorySizeCalculator _directorySizeCalculator;
    private readonly Func<string> _pathProvider;

    public WindowsTempScanner(IDirectorySizeCalculator directorySizeCalculator)
        : this(directorySizeCalculator, Path.GetTempPath)
    {
    }

    public WindowsTempScanner(IDirectorySizeCalculator directorySizeCalculator, Func<string> pathProvider)
    {
        _directorySizeCalculator = directorySizeCalculator;
        _pathProvider = pathProvider;
    }

    public string Id => ScannerId;

    public string TargetDisplayName => "Windows Temp";

    public CacheCategory TargetType => CacheCategory.TemporaryFiles;

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
            IsSafeToCleanByDefault: false);

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
