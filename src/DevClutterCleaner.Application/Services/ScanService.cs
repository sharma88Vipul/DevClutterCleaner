using DevClutterCleaner.Application.Abstractions;

namespace DevClutterCleaner.Application.Services;

public sealed class ScanService(IEnumerable<ICacheScanner> scanners)
    : CacheScanOrchestrator(scanners), IScanService
{
}
