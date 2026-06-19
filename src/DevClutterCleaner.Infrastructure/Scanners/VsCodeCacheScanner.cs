using DevClutterCleaner.Domain;
using DevClutterCleaner.Infrastructure.FileSystem;

namespace DevClutterCleaner.Infrastructure.Scanners;

public sealed class VsCodeCacheScanner : DirectoryCacheScanner
{
    public const string ScannerId = "vscode-cache";

    public VsCodeCacheScanner(IDirectorySizeCalculator directorySizeCalculator)
        : this(directorySizeCalculator, GetDefaultVsCodeCachePath)
    {
    }

    public VsCodeCacheScanner(IDirectorySizeCalculator directorySizeCalculator, Func<string> pathProvider)
        : base(directorySizeCalculator, pathProvider)
    {
    }

    public override string Id => ScannerId;

    public override string TargetDisplayName => "VS Code Cache";

    public override CacheCategory TargetType => CacheCategory.Ide;

    protected override bool IsSafeToCleanByDefault => true;

    private static string GetDefaultVsCodeCachePath()
    {
        string applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(applicationData, "Code", "Cache");
    }
}
