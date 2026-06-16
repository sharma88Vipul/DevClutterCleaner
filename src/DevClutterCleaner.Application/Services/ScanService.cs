using DevClutterCleaner.Application.Abstractions;
using DevClutterCleaner.Domain;

namespace DevClutterCleaner.Application.Services;

public sealed class ScanService(IEnumerable<ICacheScanner> scanners) : IScanService
{
    private readonly IReadOnlyList<ICacheScanner> _scanners = scanners.ToArray();

    public IReadOnlyList<ScanResult> ScanAll()
    {
        List<ScanResult> results = [];

        foreach (ICacheScanner scanner in _scanners)
        {
            try
            {
                results.Add(scanner.Scan());
            }
            catch (Exception ex)
            {
                results.Add(new ScanResult(
                    new CacheTarget(scanner.Id, scanner.Id, CacheCategory.Other, string.Empty, false),
                    0,
                    Exists: false,
                    ErrorMessage: ex.Message));
            }
        }

        return results;
    }
}
