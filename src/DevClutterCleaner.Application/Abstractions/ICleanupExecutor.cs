using DevClutterCleaner.Domain;

namespace DevClutterCleaner.Application.Abstractions;

public interface ICleanupExecutor
{
    Task CleanupAsync(CleanupPlanItem planItem, CancellationToken cancellationToken);
}
