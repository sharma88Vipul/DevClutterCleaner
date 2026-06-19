namespace DevClutterCleaner.Application.Abstractions;

public interface ICacheScannerPlugin
{
    string Name { get; }

    IReadOnlyCollection<ICacheScanner> CreateScanners(IServiceProvider services);
}
