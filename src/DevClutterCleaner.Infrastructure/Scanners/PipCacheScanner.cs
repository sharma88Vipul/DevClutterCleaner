using DevClutterCleaner.Domain;
using DevClutterCleaner.Infrastructure.FileSystem;

namespace DevClutterCleaner.Infrastructure.Scanners;

public sealed class PipCacheScanner : DirectoryCacheScanner
{
    public const string ScannerId = "pip-cache";

    public PipCacheScanner(IDirectorySizeCalculator directorySizeCalculator)
        : this(directorySizeCalculator, GetDefaultPipCachePath)
    {
    }

    public PipCacheScanner(IDirectorySizeCalculator directorySizeCalculator, Func<string> pathProvider)
        : base(directorySizeCalculator, pathProvider)
    {
    }

    public override string Id => ScannerId;

    public override string TargetDisplayName => "pip Cache";

    public override CacheCategory TargetType => CacheCategory.PackageManager;

    protected override bool IsSafeToCleanByDefault => true;

    private static string GetDefaultPipCachePath()
    {
        string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localApplicationData, "pip", "Cache");
    }
}
