using DevClutterCleaner.Application.Abstractions;

namespace DevClutterCleaner.Infrastructure.Plugins;

public sealed class PluginCacheScannerProvider(IEnumerable<ICacheScannerPlugin> plugins, IServiceProvider services)
{
    public IReadOnlyCollection<ICacheScanner> CreateScanners()
    {
        return plugins
            .SelectMany(plugin => plugin.CreateScanners(services))
            .GroupBy(scanner => scanner.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }
}
