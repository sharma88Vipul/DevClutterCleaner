using DevClutterCleaner.Application.Abstractions;
using DevClutterCleaner.Domain;

namespace DevClutterCleaner.Application.Services;

public sealed class CleanupService(
    ICleanupPlanner cleanupPlanner,
    ICleanupExecutor cleanupExecutor) : ICleanupService
{
    public async Task<IReadOnlyList<CleanupResult>> CleanupAsync(
        IEnumerable<ScanResult> scanResults,
        CleanupPolicy policy,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scanResults);
        ArgumentNullException.ThrowIfNull(policy);

        IReadOnlyList<CleanupPlanItem> planItems = cleanupPlanner.CreatePlan(scanResults, policy);
        List<CleanupResult> results = [];

        foreach (CleanupPlanItem planItem in planItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!planItem.IsEligible)
            {
                results.Add(new CleanupResult(planItem, Succeeded: false, WasDryRun: policy.Mode is CleanupMode.DryRun, ReclaimedBytes: 0, planItem.Reason));
                continue;
            }

            if (policy.Mode is CleanupMode.DryRun)
            {
                results.Add(new CleanupResult(planItem, Succeeded: true, WasDryRun: true, ReclaimedBytes: 0));
                continue;
            }

            try
            {
                await cleanupExecutor.CleanupAsync(planItem, cancellationToken);
                results.Add(new CleanupResult(planItem, Succeeded: true, WasDryRun: false, ReclaimedBytes: planItem.SizeInBytes));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                results.Add(new CleanupResult(planItem, Succeeded: false, WasDryRun: false, ReclaimedBytes: 0, ex.Message));
            }
        }

        return results;
    }
}
