using DevClutterCleaner.Domain;
using DevClutterCleaner.Infrastructure.FileSystem;

namespace DevClutterCleaner.Infrastructure.Scanners;

public sealed class DockerCacheScanner : DirectoryCacheScanner
{
    public const string ScannerId = "docker-desktop-data";

    public DockerCacheScanner(IDirectorySizeCalculator directorySizeCalculator)
        : this(directorySizeCalculator, GetDefaultDockerDataPath)
    {
    }

    public DockerCacheScanner(IDirectorySizeCalculator directorySizeCalculator, Func<string> pathProvider)
        : base(directorySizeCalculator, pathProvider)
    {
    }

    public override string Id => ScannerId;

    public override string TargetDisplayName => "Docker Desktop Data";

    public override CacheCategory TargetType => CacheCategory.Container;

    protected override bool IsSafeToCleanByDefault => false;

    private static string GetDefaultDockerDataPath()
    {
        string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localApplicationData, "Docker");
    }
}
