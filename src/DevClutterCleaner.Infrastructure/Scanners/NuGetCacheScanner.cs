using DevClutterCleaner.Application.Abstractions;
using DevClutterCleaner.Domain;
using DevClutterCleaner.Infrastructure.FileSystem;

namespace DevClutterCleaner.Infrastructure.Scanners;

public sealed class NuGetCacheScanner : ICacheScanner
{
    public const string ScannerId = "nuget-global-packages";

    private readonly IDirectorySizeCalculator _directorySizeCalculator;
    private readonly Func<string> _pathProvider;

    public NuGetCacheScanner(IDirectorySizeCalculator directorySizeCalculator)
        : this(directorySizeCalculator, GetDefaultNuGetPackagesPath)
    {
    }

    public NuGetCacheScanner(IDirectorySizeCalculator directorySizeCalculator, Func<string> pathProvider)
    {
        _directorySizeCalculator = directorySizeCalculator;
        _pathProvider = pathProvider;
    }

    public string Id => ScannerId;

    public string TargetDisplayName => "NuGet Cache";

    public CacheCategory TargetType => CacheCategory.PackageManager;

    public string GetTargetPath() => _pathProvider();

    public Task<ScanResult> ScanAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string path = GetTargetPath();
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
            long size = _directorySizeCalculator.CalculateSize(path, cancellationToken);
            return Task.FromResult(new ScanResult(target, size, Exists: true));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(new ScanResult(target, 0, Exists: true, ErrorMessage: ex.Message));
        }
    }

    private static string GetDefaultNuGetPackagesPath()
    {
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".nuget", "packages");
    }
}
