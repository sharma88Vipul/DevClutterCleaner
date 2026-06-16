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

    public ScanResult Scan()
    {
        string path = _pathProvider();
        CacheTarget target = new(
            ScannerId,
            "NuGet Cache",
            CacheCategory.PackageManager,
            path,
            IsSafeToCleanByDefault: true);

        if (!Directory.Exists(path))
        {
            return new ScanResult(target, 0, Exists: false);
        }

        try
        {
            long size = _directorySizeCalculator.CalculateSize(path);
            return new ScanResult(target, size, Exists: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new ScanResult(target, 0, Exists: true, ErrorMessage: ex.Message);
        }
    }

    private static string GetDefaultNuGetPackagesPath()
    {
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".nuget", "packages");
    }
}
