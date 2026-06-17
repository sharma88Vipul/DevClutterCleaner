namespace DevClutterCleaner.Domain;

public sealed record CleanupResult(
    CleanupPlanItem PlanItem,
    bool Succeeded,
    bool WasDryRun,
    long ReclaimedBytes,
    string? ErrorMessage = null);
