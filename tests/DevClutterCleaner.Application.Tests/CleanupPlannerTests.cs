using DevClutterCleaner.Application.Services;
using DevClutterCleaner.Domain;

namespace DevClutterCleaner.Application.Tests;

public sealed class CleanupPlannerTests
{
    private readonly CleanupPlanner _planner = new();

    [Fact]
    public void CreatePlan_MarksSafeExistingTargetsEligible()
    {
        ScanResult scanResult = CreateScanResult(isSafeToCleanByDefault: true);

        CleanupPlanItem item = Assert.Single(_planner.CreatePlan([scanResult], new CleanupPolicy()));

        Assert.True(item.IsEligible);
        Assert.Equal(RiskClassification.Low, item.Risk);
        Assert.Equal("Eligible for cleanup.", item.Reason);
    }

    [Fact]
    public void CreatePlan_BlocksMissingTargets()
    {
        ScanResult scanResult = CreateScanResult(exists: false);

        CleanupPlanItem item = Assert.Single(_planner.CreatePlan([scanResult], new CleanupPolicy()));

        Assert.False(item.IsEligible);
        Assert.Equal(RiskClassification.Blocked, item.Risk);
        Assert.Equal("Target folder does not exist.", item.Reason);
    }

    [Fact]
    public void CreatePlan_BlocksExcludedTargets()
    {
        string path = Path.Combine(Path.GetTempPath(), "dcc-excluded");
        ScanResult scanResult = CreateScanResult(path: Path.Combine(path, "child"));
        CleanupPolicy policy = new(ExcludedPaths: [path]);

        CleanupPlanItem item = Assert.Single(_planner.CreatePlan([scanResult], policy));

        Assert.False(item.IsEligible);
        Assert.Equal("Target path is excluded by policy.", item.Reason);
    }

    [Fact]
    public void CreatePlan_BlocksHighRiskTargetsUnlessExplicitlyAllowed()
    {
        ScanResult scanResult = CreateScanResult(isSafeToCleanByDefault: false);

        CleanupPlanItem blocked = Assert.Single(_planner.CreatePlan([scanResult], new CleanupPolicy()));
        CleanupPlanItem allowed = Assert.Single(_planner.CreatePlan([scanResult], new CleanupPolicy(AllowHighRiskTargets: true)));

        Assert.False(blocked.IsEligible);
        Assert.Equal(RiskClassification.High, blocked.Risk);
        Assert.True(allowed.IsEligible);
    }

    private static ScanResult CreateScanResult(
        bool exists = true,
        bool isSafeToCleanByDefault = true,
        string? path = null)
    {
        CacheTarget target = new(
            "test-cache",
            "Test Cache",
            CacheCategory.Other,
            path ?? Path.Combine(Path.GetTempPath(), "dcc-cleanup-test"),
            isSafeToCleanByDefault);

        return new ScanResult(target, 128, exists);
    }
}
