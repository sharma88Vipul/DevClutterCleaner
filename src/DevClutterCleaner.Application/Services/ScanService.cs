using DevClutterCleaner.Application.Abstractions;
using DevClutterCleaner.Domain;

namespace DevClutterCleaner.Application.Services;

public sealed class ScanService(IEnumerable<ICacheScanner> scanners) : IScanService
{
    private readonly IReadOnlyList<ICacheScanner> _scanners = scanners.ToArray();

    public async Task<IReadOnlyList<ScanResult>> ScanAllAsync(CancellationToken cancellationToken)
    {
        List<ScanResult> results = [];

        foreach (ICacheScanner scanner in _scanners)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                results.Add(await scanner.ScanAsync(cancellationToken));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                results.Add(new ScanResult(
                    new CacheTarget(scanner.Id, scanner.Id, scanner.TargetType, string.Empty, false),
                    0,
                    Exists: false,
                    ErrorMessage: ex.Message));
            }
        }

        return results;
    }
}
