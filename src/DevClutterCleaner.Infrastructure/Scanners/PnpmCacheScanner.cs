using DevClutterCleaner.Domain;
using DevClutterCleaner.Infrastructure.FileSystem;

namespace DevClutterCleaner.Infrastructure.Scanners;

public sealed class PnpmCacheScanner : DirectoryCacheScanner
{
    public const string ScannerId = "pnpm-store";

    public PnpmCacheScanner(IDirectorySizeCalculator directorySizeCalculator)
        : this(directorySizeCalculator, GetDefaultPnpmStorePath)
    {
    }

    public PnpmCacheScanner(IDirectorySizeCalculator directorySizeCalculator, Func<string> pathProvider)
        : base(directorySizeCalculator, pathProvider)
    {
    }

    public override string Id => ScannerId;

    public override string TargetDisplayName => "pnpm Store";

    public override CacheCategory TargetType => CacheCategory.PackageManager;

    protected override bool IsSafeToCleanByDefault => true;

    private static string GetDefaultPnpmStorePath()
    {
        string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localApplicationData, "pnpm", "store");
    }
}
