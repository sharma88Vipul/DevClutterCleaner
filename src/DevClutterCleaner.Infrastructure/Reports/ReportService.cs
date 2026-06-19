using System.Globalization;
using System.Net;
using System.Text;
using DevClutterCleaner.Application.Abstractions;
using DevClutterCleaner.Application.Formatting;
using DevClutterCleaner.Domain;

namespace DevClutterCleaner.Infrastructure.Reports;

public sealed class ReportService : IReportService
{
    public async Task ExportCsvAsync(
        IEnumerable<ScanResult> scanResults,
        IEnumerable<CleanupPlanItem> planItems,
        string outputPath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scanResults);
        ArgumentNullException.ThrowIfNull(planItems);
        ValidateOutputPath(outputPath);

        Dictionary<string, CleanupPlanItem> planByTargetId = planItems.ToDictionary(item => item.Target.Id, StringComparer.OrdinalIgnoreCase);
        StringBuilder csv = new();
        csv.AppendLine("Target,Category,Path,Exists,SizeBytes,Size,Risk,Eligible,Recommendation,Error");

        foreach (ScanResult result in scanResults)
        {
            cancellationToken.ThrowIfCancellationRequested();
            planByTargetId.TryGetValue(result.Target.Id, out CleanupPlanItem? planItem);

            AppendCsvRow(csv,
                result.Target.DisplayName,
                result.Target.Category.ToString(),
                result.Target.Path,
                result.Exists.ToString(CultureInfo.InvariantCulture),
                result.SizeInBytes.ToString(CultureInfo.InvariantCulture),
                FileSizeFormatter.Format(result.SizeInBytes),
                planItem?.Risk.ToString() ?? string.Empty,
                planItem?.IsEligible.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                planItem?.Reason ?? string.Empty,
                result.ErrorMessage ?? string.Empty);
        }

        await WriteTextAsync(outputPath, csv.ToString(), cancellationToken);
    }

    public async Task ExportHtmlAsync(
        IEnumerable<ScanResult> scanResults,
        IEnumerable<CleanupPlanItem> planItems,
        string outputPath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scanResults);
        ArgumentNullException.ThrowIfNull(planItems);
        ValidateOutputPath(outputPath);

        ScanResult[] results = scanResults.ToArray();
        Dictionary<string, CleanupPlanItem> planByTargetId = planItems.ToDictionary(item => item.Target.Id, StringComparer.OrdinalIgnoreCase);
        long totalEligibleBytes = planItems.Where(item => item.IsEligible).Sum(item => item.SizeInBytes);

        StringBuilder html = new();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"><title>DevClutterCleaner Report</title>");
        html.AppendLine("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:32px;color:#1f2933}table{border-collapse:collapse;width:100%}th,td{border:1px solid #d5dce5;padding:8px;text-align:left}th{background:#f1f5f9}.metric{font-size:20px;font-weight:600}</style>");
        html.AppendLine("</head><body>");
        html.AppendLine("<h1>DevClutterCleaner Report</h1>");
        html.AppendLine(CultureInfo.InvariantCulture, $"<p>Generated: {WebUtility.HtmlEncode(FormatGeneratedTimestamp())}</p>");
        html.AppendLine(CultureInfo.InvariantCulture, $"<p class=\"metric\">Eligible reclaimable space: {WebUtility.HtmlEncode(FileSizeFormatter.Format(totalEligibleBytes))}</p>");
        html.AppendLine("<table><thead><tr><th>Target</th><th>Category</th><th>Path</th><th>Exists</th><th>Size</th><th>Risk</th><th>Eligible</th><th>Recommendation</th></tr></thead><tbody>");

        foreach (ScanResult result in results)
        {
            cancellationToken.ThrowIfCancellationRequested();
            planByTargetId.TryGetValue(result.Target.Id, out CleanupPlanItem? planItem);

            html.AppendLine("<tr>");
            AppendHtmlCell(html, result.Target.DisplayName);
            AppendHtmlCell(html, result.Target.Category.ToString());
            AppendHtmlCell(html, result.Target.Path);
            AppendHtmlCell(html, result.Exists ? "Yes" : "No");
            AppendHtmlCell(html, FileSizeFormatter.Format(result.SizeInBytes));
            AppendHtmlCell(html, planItem?.Risk.ToString() ?? string.Empty);
            AppendHtmlCell(html, planItem?.IsEligible is true ? "Yes" : "No");
            AppendHtmlCell(html, planItem?.Reason ?? result.ErrorMessage ?? string.Empty);
            html.AppendLine("</tr>");
        }

        html.AppendLine("</tbody></table></body></html>");
        await WriteTextAsync(outputPath, html.ToString(), cancellationToken);
    }

    public async Task ExportPdfAsync(
        IEnumerable<ScanResult> scanResults,
        IEnumerable<CleanupPlanItem> planItems,
        string outputPath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scanResults);
        ArgumentNullException.ThrowIfNull(planItems);
        ValidateOutputPath(outputPath);

        ScanResult[] results = scanResults.ToArray();
        CleanupPlanItem[] plans = planItems.ToArray();
        Dictionary<string, CleanupPlanItem> planByTargetId = plans.ToDictionary(item => item.Target.Id, StringComparer.OrdinalIgnoreCase);
        long totalEligibleBytes = plans.Where(item => item.IsEligible).Sum(item => item.SizeInBytes);

        List<string> lines =
        [
            "DevClutterCleaner Report",
            $"Generated: {FormatGeneratedTimestamp()}",
            $"Eligible reclaimable space: {FileSizeFormatter.Format(totalEligibleBytes)}",
            string.Empty
        ];

        foreach (ScanResult result in results)
        {
            cancellationToken.ThrowIfCancellationRequested();
            planByTargetId.TryGetValue(result.Target.Id, out CleanupPlanItem? planItem);
            lines.Add($"{result.Target.DisplayName} | {result.Target.Category} | {FileSizeFormatter.Format(result.SizeInBytes)} | Risk: {planItem?.Risk.ToString() ?? "Unknown"} | Eligible: {(planItem?.IsEligible is true ? "Yes" : "No")}");
            lines.Add($"Path: {result.Target.Path}");
            lines.Add($"Recommendation: {planItem?.Reason ?? result.ErrorMessage ?? string.Empty}");
            lines.Add(string.Empty);
        }

        byte[] pdfBytes = BuildSimplePdf(lines);
        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(outputPath, pdfBytes, cancellationToken);
    }

    private static void AppendCsvRow(StringBuilder csv, params string[] values)
    {
        csv.AppendLine(string.Join(",", values.Select(EscapeCsv)));
    }

    private static string EscapeCsv(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static void AppendHtmlCell(StringBuilder html, string value)
    {
        html.Append("<td>");
        html.Append(WebUtility.HtmlEncode(value));
        html.Append("</td>");
    }

    private static string FormatGeneratedTimestamp()
    {
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.CurrentCulture);
    }

    private static byte[] BuildSimplePdf(IReadOnlyList<string> lines)
    {
        StringBuilder content = new();
        content.AppendLine("BT");
        content.AppendLine("/F1 11 Tf");
        content.AppendLine("50 760 Td");

        foreach (string line in lines.Take(42))
        {
            content.Append('(');
            content.Append(EscapePdfText(line.Length > 110 ? line[..110] : line));
            content.AppendLine(") Tj");
            content.AppendLine("0 -16 Td");
        }

        content.AppendLine("ET");

        string contentStream = content.ToString();
        List<string> objects =
        [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(contentStream)} >>\nstream\n{contentStream}endstream"
        ];

        StringBuilder pdf = new();
        pdf.AppendLine("%PDF-1.4");
        List<int> offsets = [];

        for (int index = 0; index < objects.Count; index++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));
            pdf.AppendLine(CultureInfo.InvariantCulture, $"{index + 1} 0 obj");
            pdf.AppendLine(objects[index]);
            pdf.AppendLine("endobj");
        }

        int xrefOffset = Encoding.ASCII.GetByteCount(pdf.ToString());
        pdf.AppendLine("xref");
        pdf.AppendLine(CultureInfo.InvariantCulture, $"0 {objects.Count + 1}");
        pdf.AppendLine("0000000000 65535 f ");

        foreach (int offset in offsets)
        {
            pdf.AppendLine(CultureInfo.InvariantCulture, $"{offset:0000000000} 00000 n ");
        }

        pdf.AppendLine("trailer");
        pdf.AppendLine(CultureInfo.InvariantCulture, $"<< /Size {objects.Count + 1} /Root 1 0 R >>");
        pdf.AppendLine("startxref");
        pdf.AppendLine(CultureInfo.InvariantCulture, $"{xrefOffset}");
        pdf.AppendLine("%%EOF");

        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private static string EscapePdfText(string value)
    {
        StringBuilder escaped = new(value.Length);
        foreach (char character in value)
        {
            escaped.Append(character switch
            {
                '(' => "\\(",
                ')' => "\\)",
                '\\' => "\\\\",
                >= ' ' and <= '~' => character,
                _ => '?'
            });
        }

        return escaped.ToString();
    }

    private static async Task WriteTextAsync(string outputPath, string content, CancellationToken cancellationToken)
    {
        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(outputPath, content, Encoding.UTF8, cancellationToken);
    }

    private static void ValidateOutputPath(string outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path is required.", nameof(outputPath));
        }
    }
}
