namespace DevClutterCleaner.Domain;

public sealed record AuditEntry(
    DateTimeOffset Timestamp,
    AuditAction Action,
    string Message,
    string? TargetId = null,
    string? TargetPath = null,
    long? SizeInBytes = null,
    bool Succeeded = true,
    string? ErrorMessage = null);
