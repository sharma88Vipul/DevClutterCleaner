using System.Text.Json;
using DevClutterCleaner.Domain;
using DevClutterCleaner.Infrastructure.Audit;

namespace DevClutterCleaner.Infrastructure.Tests;

public sealed class FileAuditServiceTests
{
    [Fact]
    public async Task RecordAsync_AppendsJsonLine()
    {
        string outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "audit.jsonl");
        FileAuditService service = new(outputPath);
        AuditEntry entry = new(DateTimeOffset.UtcNow, AuditAction.ScanCompleted, "Scan complete.", SizeInBytes: 42);

        await service.RecordAsync(entry, CancellationToken.None);

        string jsonLine = await File.ReadAllTextAsync(outputPath);
        AuditEntry? roundTripped = JsonSerializer.Deserialize<AuditEntry>(jsonLine);
        Assert.NotNull(roundTripped);
        Assert.Equal(AuditAction.ScanCompleted, roundTripped.Action);
        Assert.Equal(42, roundTripped.SizeInBytes);
    }
}
