using DevClutterCleaner.Domain;

namespace DevClutterCleaner.Application.Abstractions;

public interface ICleanupService
{
    Task<IReadOnlyList<CleanupResult>> CleanupAsync(
        IEnumerable<ScanResult> scanResults,
        CleanupPolicy policy,
        CancellationToken cancellationToken);
}
