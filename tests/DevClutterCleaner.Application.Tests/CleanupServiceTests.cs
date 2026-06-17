using DevClutterCleaner.Application.Abstractions;
using DevClutterCleaner.Application.Services;
using DevClutterCleaner.Domain;

namespace DevClutterCleaner.Application.Tests;

public sealed class CleanupServiceTests
{
    [Fact]
    public async Task CleanupAsync_DoesNotExecuteCleanup_WhenPolicyIsDryRun()
    {
        RecordingCleanupExecutor executor = new();
        CleanupService service = new(new CleanupPlanner(), executor);

        IReadOnlyList<CleanupResult> results = await service.CleanupAsync(
            [CreateScanResult()],
            new CleanupPolicy(),
            CancellationToken.None);

        CleanupResult result = Assert.Single(results);
        Assert.True(result.Succeeded);
        Assert.True(result.WasDryRun);
        Assert.Equal(0, result.ReclaimedBytes);
        Assert.Equal(0, executor.CallCount);
    }

    [Fact]
    public async Task CleanupAsync_ExecutesCleanup_WhenPolicyAllowsRecycleBinMode()
    {
        RecordingCleanupExecutor executor = new();
        CleanupService service = new(new CleanupPlanner(), executor);

        IReadOnlyList<CleanupResult> results = await service.CleanupAsync(
            [CreateScanResult()],
            new CleanupPolicy(Mode: CleanupMode.MoveToRecycleBin),
            CancellationToken.None);

        CleanupResult result = Assert.Single(results);
        Assert.True(result.Succeeded);
        Assert.False(result.WasDryRun);
        Assert.Equal(128, result.ReclaimedBytes);
        Assert.Equal(1, executor.CallCount);
    }

    [Fact]
    public async Task CleanupAsync_DoesNotExecuteCleanup_ForIneligiblePlanItems()
    {
        RecordingCleanupExecutor executor = new();
        CleanupService service = new(new CleanupPlanner(), executor);

        IReadOnlyList<CleanupResult> results = await service.CleanupAsync(
            [CreateScanResult(exists: false)],
            new CleanupPolicy(Mode: CleanupMode.MoveToRecycleBin),
            CancellationToken.None);

        CleanupResult result = Assert.Single(results);
        Assert.False(result.Succeeded);
        Assert.Equal(0, executor.CallCount);
    }

    private static ScanResult CreateScanResult(bool exists = true)
    {
        CacheTarget target = new(
            "test-cache",
            "Test Cache",
            CacheCategory.Other,
            Path.Combine(Path.GetTempPath(), "dcc-cleanup-service-test"),
            IsSafeToCleanByDefault: true);

        return new ScanResult(target, 128, exists);
    }

    private sealed class RecordingCleanupExecutor : ICleanupExecutor
    {
        public int CallCount { get; private set; }

        public Task CleanupAsync(CleanupPlanItem planItem, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }
}
