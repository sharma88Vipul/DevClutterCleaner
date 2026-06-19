using DevClutterCleaner.Domain;

namespace DevClutterCleaner.Application.Abstractions;

public interface IAuditService
{
    Task RecordAsync(AuditEntry entry, CancellationToken cancellationToken);
}
