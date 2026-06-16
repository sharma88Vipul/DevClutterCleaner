namespace DevClutterCleaner.Domain;

public sealed record ScanResult(
    CacheTarget Target,
    long SizeInBytes,
    bool Exists,
    string? ErrorMessage = null);
