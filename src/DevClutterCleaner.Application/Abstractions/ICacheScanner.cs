using DevClutterCleaner.Domain;

namespace DevClutterCleaner.Application.Abstractions;

public interface ICacheScanner
{
    string Id { get; }

    CacheCategory TargetType { get; }

    Task<ScanResult> ScanAsync(CancellationToken cancellationToken);
}
