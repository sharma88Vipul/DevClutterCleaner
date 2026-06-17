namespace DevClutterCleaner.Domain;

public sealed record CleanupPlanItem(
    CacheTarget Target,
    long SizeInBytes,
    RiskClassification Risk,
    bool IsEligible,
    string Reason);
