using DevClutterCleaner.Application.Abstractions;
using DevClutterCleaner.Infrastructure.FileSystem;
using DevClutterCleaner.Infrastructure.Scanners;
using Microsoft.Extensions.DependencyInjection;

namespace DevClutterCleaner.Infrastructure.Plugins;

public sealed class BuiltInCacheScannerPlugin : ICacheScannerPlugin
{
    public string Name => "Built-in developer cache scanners";

    public IReadOnlyCollection<ICacheScanner> CreateScanners(IServiceProvider services)
    {
        IDirectorySizeCalculator directorySizeCalculator = services.GetRequiredService<IDirectorySizeCalculator>();

        return
        [
            new NuGetCacheScanner(directorySizeCalculator),
            new NpmCacheScanner(directorySizeCalculator),
            new PnpmCacheScanner(directorySizeCalculator),
            new PipCacheScanner(directorySizeCalculator),
            new WindowsTempScanner(directorySizeCalculator),
            new VsCodeCacheScanner(directorySizeCalculator),
            new DockerCacheScanner(directorySizeCalculator),
            new GitCacheScanner(directorySizeCalculator),
            new ClaudeCliCacheScanner(directorySizeCalculator),
            new CodexCliCacheScanner(directorySizeCalculator)
        ];
    }
}
