namespace DevClutterCleaner.Domain;

public sealed record CleanupPolicy(
    CleanupMode Mode = CleanupMode.DryRun,
    bool AllowHighRiskTargets = false,
    IReadOnlyCollection<string>? ExcludedPaths = null);
