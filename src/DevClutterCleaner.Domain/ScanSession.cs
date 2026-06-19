namespace DevClutterCleaner.Domain;

public sealed record ScanSession(
    Guid Id,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<ScanResult> Results);
