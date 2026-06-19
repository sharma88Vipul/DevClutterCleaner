using DevClutterCleaner.Domain;
using DevClutterCleaner.Infrastructure.FileSystem;

namespace DevClutterCleaner.Infrastructure.Scanners;

public sealed class ClaudeCliCacheScanner : DirectoryCacheScanner
{
    public const string ScannerId = "claude-cli-cache";

    public ClaudeCliCacheScanner(IDirectorySizeCalculator directorySizeCalculator)
        : this(directorySizeCalculator, GetDefaultClaudeCachePath)
    {
    }

    public ClaudeCliCacheScanner(IDirectorySizeCalculator directorySizeCalculator, Func<string> pathProvider)
        : base(directorySizeCalculator, pathProvider)
    {
    }

    public override string Id => ScannerId;

    public override string TargetDisplayName => "Claude CLI Cache";

    public override CacheCategory TargetType => CacheCategory.AiTooling;

    protected override bool IsSafeToCleanByDefault => false;

    private static string GetDefaultClaudeCachePath()
    {
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".claude");
    }
}
