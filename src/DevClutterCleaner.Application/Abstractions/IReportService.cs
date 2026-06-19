using DevClutterCleaner.Domain;

namespace DevClutterCleaner.Application.Abstractions;

public interface IReportService
{
    Task ExportCsvAsync(
        IEnumerable<ScanResult> scanResults,
        IEnumerable<CleanupPlanItem> planItems,
        string outputPath,
        CancellationToken cancellationToken);

    Task ExportHtmlAsync(
        IEnumerable<ScanResult> scanResults,
        IEnumerable<CleanupPlanItem> planItems,
        string outputPath,
        CancellationToken cancellationToken);

    Task ExportPdfAsync(
        IEnumerable<ScanResult> scanResults,
        IEnumerable<CleanupPlanItem> planItems,
        string outputPath,
        CancellationToken cancellationToken);
}
