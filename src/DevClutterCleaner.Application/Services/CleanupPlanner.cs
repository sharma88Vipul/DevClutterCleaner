using DevClutterCleaner.Application.Abstractions;
using DevClutterCleaner.Domain;

namespace DevClutterCleaner.Application.Services;

public sealed class CleanupPlanner : ICleanupPlanner
{
    public IReadOnlyList<CleanupPlanItem> CreatePlan(
        IEnumerable<ScanResult> scanResults,
        CleanupPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(scanResults);
        ArgumentNullException.ThrowIfNull(policy);

        return scanResults
            .Select(result => CreatePlanItem(result, policy))
            .ToArray();
    }

    private static CleanupPlanItem CreatePlanItem(ScanResult result, CleanupPolicy policy)
    {
        RiskClassification risk = ClassifyRisk(result);
        string? blockReason = GetBlockReason(result, policy, risk);

        return new CleanupPlanItem(
            result.Target,
            result.SizeInBytes,
            risk,
            IsEligible: blockReason is null,
            blockReason ?? "Eligible for cleanup.");
    }

    private static RiskClassification ClassifyRisk(ScanResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.ErrorMessage) || !result.Exists)
        {
            return RiskClassification.Blocked;
        }

        if (IsProtectedPath(result.Target.Path))
        {
            return RiskClassification.Blocked;
        }

        return result.Target.IsSafeToCleanByDefault
            ? RiskClassification.Low
            : RiskClassification.High;
    }

    private static string? GetBlockReason(
        ScanResult result,
        CleanupPolicy policy,
        RiskClassification risk)
    {
        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            return result.ErrorMessage;
        }

        if (!result.Exists)
        {
            return "Target folder does not exist.";
        }

        if (result.SizeInBytes <= 0)
        {
            return "Target has no reclaimable bytes.";
        }

        if (IsExcluded(result.Target.Path, policy.ExcludedPaths))
        {
            return "Target path is excluded by policy.";
        }

        if (risk is RiskClassification.Blocked)
        {
            return "Target path is protected.";
        }

        if (risk is RiskClassification.High && !policy.AllowHighRiskTargets)
        {
            return "High-risk target requires explicit policy approval.";
        }

        return null;
    }

    private static bool IsExcluded(string path, IReadOnlyCollection<string>? excludedPaths)
    {
        if (excludedPaths is null || excludedPaths.Count == 0)
        {
            return false;
        }

        string normalizedPath = NormalizePath(path);
        return excludedPaths
            .Where(excludedPath => !string.IsNullOrWhiteSpace(excludedPath))
            .Select(NormalizePath)
            .Any(excludedPath => normalizedPath.Equals(excludedPath, StringComparison.OrdinalIgnoreCase)
                || normalizedPath.StartsWith(excludedPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsProtectedPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return true;
        }

        string normalizedPath = NormalizePath(path);
        string? root = Path.GetPathRoot(normalizedPath);

        if (string.IsNullOrWhiteSpace(root) || normalizedPath.Equals(root.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return !string.IsNullOrWhiteSpace(userProfile)
            && normalizedPath.Equals(NormalizePath(userProfile), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
