using DevClutterCleaner.Domain;
using DevClutterCleaner.Infrastructure.Reports;

namespace DevClutterCleaner.Infrastructure.Tests;

public sealed class ReportServiceTests
{
    [Fact]
    public async Task ExportCsvAsync_WritesScanAndPlanRows()
    {
        string outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "report.csv");
        ReportService service = new();
        CacheTarget target = new("npm-cache", "npm Cache", CacheCategory.PackageManager, @"C:\cache,npm", true);
        ScanResult scanResult = new(target, 1024, Exists: true);
        CleanupPlanItem planItem = new(target, 1024, RiskClassification.Low, IsEligible: true, "Eligible for cleanup.");

        await service.ExportCsvAsync([scanResult], [planItem], outputPath, CancellationToken.None);

        string csv = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("Target,Category,Path,Exists,SizeBytes,Size,Risk,Eligible,Recommendation,Error", csv);
        Assert.Contains("\"C:\\cache,npm\"", csv);
        Assert.Contains("Low", csv);
    }

    [Fact]
    public async Task ExportHtmlAsync_WritesEscapedSummary()
    {
        string outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "report.html");
        ReportService service = new();
        CacheTarget target = new("codex-cli-cache", "Codex <CLI>", CacheCategory.AiTooling, @"C:\.codex", false);
        ScanResult scanResult = new(target, 2048, Exists: true);
        CleanupPlanItem planItem = new(target, 2048, RiskClassification.High, IsEligible: false, "High-risk target requires explicit policy approval.");

        await service.ExportHtmlAsync([scanResult], [planItem], outputPath, CancellationToken.None);

        string html = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("DevClutterCleaner Report", html);
        Assert.DoesNotContain("Z</p>", html);
        Assert.DoesNotContain("+05:30", html);
        Assert.Contains("Codex &lt;CLI&gt;", html);
        Assert.Contains("High-risk target requires explicit policy approval.", html);
    }

    [Fact]
    public async Task ExportPdfAsync_WritesPdfPayload()
    {
        string outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "report.pdf");
        ReportService service = new();
        CacheTarget target = new("nuget-global-packages", "NuGet Cache", CacheCategory.PackageManager, @"C:\.nuget\packages", true);
        ScanResult scanResult = new(target, 4096, Exists: true);
        CleanupPlanItem planItem = new(target, 4096, RiskClassification.Low, IsEligible: true, "Eligible for cleanup.");

        await service.ExportPdfAsync([scanResult], [planItem], outputPath, CancellationToken.None);

        byte[] bytes = await File.ReadAllBytesAsync(outputPath);
        string header = System.Text.Encoding.ASCII.GetString(bytes.Take(8).ToArray());
        string pdfText = System.Text.Encoding.ASCII.GetString(bytes);
        Assert.StartsWith("%PDF-1.4", header);
        Assert.DoesNotContain("Z) Tj", pdfText);
        Assert.DoesNotContain("+05:30", pdfText);
        Assert.Contains("NuGet Cache", pdfText);
    }
}
