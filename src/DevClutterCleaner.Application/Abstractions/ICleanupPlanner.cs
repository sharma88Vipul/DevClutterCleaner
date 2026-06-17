using DevClutterCleaner.Domain;

namespace DevClutterCleaner.Application.Abstractions;

public interface ICleanupPlanner
{
    IReadOnlyList<CleanupPlanItem> CreatePlan(
        IEnumerable<ScanResult> scanResults,
        CleanupPolicy policy);
}
