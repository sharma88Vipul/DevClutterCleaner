namespace DevClutterCleaner.Domain;

public sealed record CacheTarget(
    string Id,
    string DisplayName,
    CacheCategory Category,
    string Path,
    bool IsSafeToCleanByDefault);
