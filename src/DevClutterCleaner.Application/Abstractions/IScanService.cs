using DevClutterCleaner.Domain;

namespace DevClutterCleaner.Application.Abstractions;

public interface IScanService
{
    IReadOnlyList<ScanResult> ScanAll();
}
