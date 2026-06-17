using DevClutterCleaner.Domain;

namespace DevClutterCleaner.Application.Abstractions;

public interface ICacheScanner
{
    string Id { get; }

    string TargetDisplayName { get; }

    CacheCategory TargetType { get; }

    string GetTargetPath();

    Task<ScanResult> ScanAsync(CancellationToken cancellationToken);
}
