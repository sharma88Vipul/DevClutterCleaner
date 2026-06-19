namespace DevClutterCleaner.Domain;

public enum AuditAction
{
    ScanStarted,
    ScanCompleted,
    CleanupPreviewed,
    CleanupStarted,
    CleanupCompleted,
    ReportExported,
    SettingsChanged
}
