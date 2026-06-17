using DevClutterCleaner.Domain;

namespace DevClutterCleaner.Application.Abstractions;

public interface ICacheScanOrchestrator
{
    Task<IReadOnlyList<ScanResult>> ScanAllAsync(CancellationToken cancellationToken);
}
