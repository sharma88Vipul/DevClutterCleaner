using DevClutterCleaner.Domain;
using DevClutterCleaner.Infrastructure.FileSystem;

namespace DevClutterCleaner.Infrastructure.Scanners;

public sealed class GitCacheScanner : DirectoryCacheScanner
{
    public const string ScannerId = "git-credential-manager-cache";

    public GitCacheScanner(IDirectorySizeCalculator directorySizeCalculator)
        : this(directorySizeCalculator, GetDefaultGitCachePath)
    {
    }

    public GitCacheScanner(IDirectorySizeCalculator directorySizeCalculator, Func<string> pathProvider)
        : base(directorySizeCalculator, pathProvider)
    {
    }

    public override string Id => ScannerId;

    public override string TargetDisplayName => "Git Credential Cache";

    public override CacheCategory TargetType => CacheCategory.Other;

    protected override bool IsSafeToCleanByDefault => false;

    private static string GetDefaultGitCachePath()
    {
        string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localApplicationData, "GitCredentialManager");
    }
}
