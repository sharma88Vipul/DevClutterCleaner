using DevClutterCleaner.Domain;
using DevClutterCleaner.Infrastructure.FileSystem;

namespace DevClutterCleaner.Infrastructure.Scanners;

public sealed class CodexCliCacheScanner : DirectoryCacheScanner
{
    public const string ScannerId = "codex-cli-cache";

    public CodexCliCacheScanner(IDirectorySizeCalculator directorySizeCalculator)
        : this(directorySizeCalculator, GetDefaultCodexCachePath)
    {
    }

    public CodexCliCacheScanner(IDirectorySizeCalculator directorySizeCalculator, Func<string> pathProvider)
        : base(directorySizeCalculator, pathProvider)
    {
    }

    public override string Id => ScannerId;

    public override string TargetDisplayName => "Codex CLI Cache";

    public override CacheCategory TargetType => CacheCategory.AiTooling;

    protected override bool IsSafeToCleanByDefault => false;

    private static string GetDefaultCodexCachePath()
    {
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".codex");
    }
}
