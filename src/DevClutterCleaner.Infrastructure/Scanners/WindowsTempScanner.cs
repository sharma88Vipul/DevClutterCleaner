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

    public CacheCategory TargetType => CacheCategory.TemporaryFiles;

    public Task<ScanResult> ScanAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string path = _pathProvider();
        CacheTarget target = new(
            ScannerId,
            "Windows Temp",
            TargetType,
            path,
            IsSafeToCleanByDefault: false);

        if (!Directory.Exists(path))
        {
            return Task.FromResult(new ScanResult(target, 0, Exists: false));
        }

        try
        {
            long size = _directorySizeCalculator.CalculateSize(path, cancellationToken);
            return Task.FromResult(new ScanResult(target, size, Exists: true));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(new ScanResult(target, 0, Exists: true, ErrorMessage: ex.Message));
        }
    }
}
