using DevClutterCleaner.Domain;

namespace DevClutterCleaner.Application.Abstractions;

public interface ICacheScanner
{
    string Id { get; }

    ScanResult Scan();
}
