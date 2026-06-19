using System.Text.Json;
using DevClutterCleaner.Application.Abstractions;
using DevClutterCleaner.Domain;

namespace DevClutterCleaner.Infrastructure.Audit;

public sealed class FileAuditService : IAuditService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false
    };

    private readonly string _auditPath;

    public FileAuditService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DevClutterCleaner",
            "audit.jsonl"))
    {
    }

    public FileAuditService(string auditPath)
    {
        _auditPath = auditPath;
    }

    public async Task RecordAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);

        string? directory = Path.GetDirectoryName(_auditPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string line = JsonSerializer.Serialize(entry, SerializerOptions) + Environment.NewLine;
        await File.AppendAllTextAsync(_auditPath, line, cancellationToken);
    }
}
