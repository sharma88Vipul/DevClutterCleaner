using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media.Animation;
using DevClutterCleaner.Application.Abstractions;
using DevClutterCleaner.Application.Formatting;
using DevClutterCleaner.Domain;
using DevClutterCleaner.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace DevClutterCleaner.UI;

public partial class MainWindow : Window
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IReadOnlyList<ICacheScanner> _scanners;
    private readonly ICleanupPlanner _cleanupPlanner;
    private readonly ICleanupService _cleanupService;
    private readonly IReportService _reportService;
    private readonly IAuditService _auditService;
    private readonly ILicenseService _licenseService;
    private readonly AppSettingsStore _settingsStore = new();
    private readonly ObservableCollection<ScanResultRow> _rows = [];
    private readonly ObservableCollection<string> _excludedPaths = [];
    private readonly List<ScanResult> _scanResults = [];

    public MainWindow()
    {
        InitializeComponent();

        _serviceProvider = new ServiceCollection()
            .AddDevClutterCleanerInfrastructure()
            .BuildServiceProvider();
        _scanners = _serviceProvider.GetServices<ICacheScanner>().ToArray();
        _cleanupPlanner = _serviceProvider.GetRequiredService<ICleanupPlanner>();
        _cleanupService = _serviceProvider.GetRequiredService<ICleanupService>();
        _reportService = _serviceProvider.GetRequiredService<IReportService>();
        _auditService = _serviceProvider.GetRequiredService<IAuditService>();
        _licenseService = _serviceProvider.GetRequiredService<ILicenseService>();
        ResultsGrid.ItemsSource = _rows;
        ExclusionsList.ItemsSource = _excludedPaths;
        LoadSettings();
        ScanCountText.Text = $"0 / {_scanners.Count}";
        LicenseStatus licenseStatus = _licenseService.GetCurrentStatus();
        StatusText.Text = $"{licenseStatus.Edition} edition. Telemetry is disabled.";
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        await RunScanAsync();
    }

    private async Task RunScanAsync()
    {
        ScanButton.IsEnabled = false;
        CleanSelectedButton.IsEnabled = false;
        ScanButtonText.Text = "Scanning";
        StatusText.Text = string.Empty;
        CurrentScanText.Text = "Preparing scanner targets...";
        ScanActivityText.Text = "Starting scan";
        CurrentFileText.Text = "Preparing scanner targets...";
        ScanProgressBar.Visibility = Visibility.Visible;
        ScanProgressBar.IsIndeterminate = true;
        TotalReclaimableResultText.Text = "Total reclaimable space: Scanning...";
        TotalReclaimableMetricText.Text = "-";
        TargetsScannedMetricText.Text = "0";
        LowRiskMetricText.Text = "0";
        NeedsReviewMetricText.Text = "0";
        PreviewCleanupButton.IsEnabled = false;
        ExportCsvButton.IsEnabled = false;
        ExportHtmlButton.IsEnabled = false;
        ExportPdfButton.IsEnabled = false;
        AddSelectedExclusionButton.IsEnabled = false;
        _rows.Clear();
        _scanResults.Clear();
        StartScanLoader();
        await RecordAuditAsync(new AuditEntry(DateTimeOffset.UtcNow, AuditAction.ScanStarted, "Scan started."));

        foreach (ICacheScanner scanner in _scanners)
        {
            _rows.Add(CreatePendingRow(scanner));
        }

        try
        {
            List<ScanResult> results = [];

            for (int index = 0; index < _scanners.Count; index++)
            {
                ICacheScanner scanner = _scanners[index];
                ScanResultRow row = _rows[index];
                row.Status = "Scanning";
                row.Path = scanner.GetTargetPath();
                CurrentScanText.Text = $"Scanning {scanner.TargetDisplayName}: {row.Path}";
                ScanActivityText.Text = $"Scanning {scanner.TargetDisplayName}";
                CurrentFileText.Text = row.Path;
                ScanCountText.Text = $"{index} / {_scanners.Count}";
                ResultsGrid.Items.Refresh();

                IProgress<ScanProgress> progress = new Progress<ScanProgress>(scanProgress =>
                {
                    ScanActivityText.Text = $"Scanning {scanProgress.TargetName}";
                    CurrentFileText.Text = scanProgress.CurrentPath;
                });

                await Task.Delay(150);

                try
                {
                    ScanResult result = await Task.Run(
                        () => scanner.ScanAsync(CancellationToken.None, progress),
                        CancellationToken.None);
                    results.Add(result);
                    _scanResults.Add(result);
                    ApplyResult(row, result);
                    ApplyPlan(row, result, BuildCleanupPolicy());
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    row.Status = ex.Message;
                    row.Exists = "No";
                    row.Size = "0 B";
                    row.Risk = RiskClassification.Blocked.ToString();
                    row.Recommendation = "Review scanner error before cleanup.";
                }

                ScanCountText.Text = $"{index + 1} / {_scanners.Count}";
                UpdateSummary(results);
                ResultsGrid.Items.Refresh();
            }

            UpdateSummary(results);
            PreviewCleanupButton.IsEnabled = _scanResults.Count > 0;
            ExportCsvButton.IsEnabled = _scanResults.Count > 0;
            ExportHtmlButton.IsEnabled = _scanResults.Count > 0;
            ExportPdfButton.IsEnabled = _scanResults.Count > 0;
            UpdateCleanSelectedButton();
            SetLastScan(DateTime.Now);
            CurrentScanText.Text = "Scan complete.";
            ScanActivityText.Text = "Scan complete";
            CurrentFileText.Text = "All configured cache targets were scanned.";
            await RecordAuditAsync(new AuditEntry(
                DateTimeOffset.UtcNow,
                AuditAction.ScanCompleted,
                $"Scan completed with {_scanResults.Count} target(s).",
                SizeInBytes: _scanResults.Sum(result => result.SizeInBytes)));
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Scan canceled.";
            CurrentScanText.Text = "Scan canceled.";
            ScanActivityText.Text = "Canceled";
            CurrentFileText.Text = "Scan canceled before completion.";
        }
        finally
        {
            ScanProgressBar.IsIndeterminate = false;
            ScanProgressBar.Visibility = Visibility.Collapsed;
            ScanButtonText.Text = "Scan caches";
            ScanButton.IsEnabled = true;
            StopScanLoader();
        }
    }

    private static ScanResultRow CreatePendingRow(ICacheScanner scanner)
    {
        return new ScanResultRow
        {
            Name = scanner.TargetDisplayName,
            Category = scanner.TargetType.ToString(),
            Path = scanner.GetTargetPath(),
            Exists = "-",
            Size = "-",
            Risk = "-",
            Status = "Pending",
            Recommendation = "Waiting for scan result."
        };
    }

    private static void ApplyResult(ScanResultRow row, ScanResult result)
    {
        string status = "Scanned";
        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            status = result.ErrorMessage;
        }
        else if (!result.Exists)
        {
            status = "Not found";
        }

        row.Name = result.Target.DisplayName;
        row.Category = result.Target.Category.ToString();
        row.Path = result.Target.Path;
        row.Exists = result.Exists ? "Yes" : "No";
        row.Size = FileSizeFormatter.Format(result.SizeInBytes);
        row.Status = status;
    }

    private void ApplyPlan(ScanResultRow row, ScanResult result)
    {
        ApplyPlan(row, result, BuildCleanupPolicy());
    }

    private void ApplyPlan(ScanResultRow row, ScanResult result, CleanupPolicy policy)
    {
        CleanupPlanItem planItem = _cleanupPlanner.CreatePlan([result], policy).Single();

        row.Risk = planItem.Risk.ToString();
        row.Recommendation = planItem.IsEligible
            ? $"Dry-run eligible: {FileSizeFormatter.Format(planItem.SizeInBytes)}."
            : planItem.Reason;
        row.CanSelectForCleanup = planItem.IsEligible;
        row.IsSelectedForCleanup = row.IsSelectedForCleanup && row.CanSelectForCleanup;
    }

    private void UpdateSummary(IReadOnlyCollection<ScanResult> results)
    {
        CleanupPolicy policy = BuildCleanupPolicy();
        IReadOnlyList<CleanupPlanItem> planItems = _cleanupPlanner.CreatePlan(results, policy);
        long totalBytes = planItems
            .Where(item => item.IsEligible)
            .Sum(item => item.SizeInBytes);
        int lowRiskCount = planItems.Count(item => item.Risk is RiskClassification.Low && item.IsEligible);
        int needsReviewCount = planItems.Count(item => !item.IsEligible || item.Risk is RiskClassification.High or RiskClassification.Blocked);

        string total = FileSizeFormatter.Format(totalBytes);
        TotalReclaimableMetricText.Text = total;
        TargetsScannedMetricText.Text = results.Count.ToString();
        LowRiskMetricText.Text = lowRiskCount.ToString();
        NeedsReviewMetricText.Text = needsReviewCount.ToString();
        TotalReclaimableResultText.Text = $"Total reclaimable space: {total}";
    }

    private void PreviewCleanupButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshCleanupPreview();
    }

    private async void ExportCsvButton_Click(object sender, RoutedEventArgs e)
    {
        await ExportReportAsync("CSV report (*.csv)|*.csv", "csv", _reportService.ExportCsvAsync);
    }

    private async void ExportHtmlButton_Click(object sender, RoutedEventArgs e)
    {
        await ExportReportAsync("HTML report (*.html)|*.html", "html", _reportService.ExportHtmlAsync);
    }

    private async void ExportPdfButton_Click(object sender, RoutedEventArgs e)
    {
        await ExportReportAsync("PDF report (*.pdf)|*.pdf", "pdf", _reportService.ExportPdfAsync);
    }

    private async Task ExportReportAsync(
        string filter,
        string defaultExtension,
        Func<IEnumerable<ScanResult>, IEnumerable<CleanupPlanItem>, string, CancellationToken, Task> export)
    {
        if (_scanResults.Count == 0)
        {
            return;
        }

        SaveFileDialog dialog = new()
        {
            AddExtension = true,
            DefaultExt = defaultExtension,
            FileName = $"DevClutterCleaner-report-{DateTime.Now:yyyyMMdd-HHmm}",
            Filter = filter,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) is not true)
        {
            return;
        }

        try
        {
            SetCleanupUiEnabled(false);
            ExportCsvButton.IsEnabled = false;
            ExportHtmlButton.IsEnabled = false;
            ExportPdfButton.IsEnabled = false;

            CleanupPolicy policy = BuildCleanupPolicy();
            IReadOnlyList<CleanupPlanItem> planItems = _cleanupPlanner.CreatePlan(_scanResults, policy);
            await export(_scanResults, planItems, dialog.FileName, CancellationToken.None);

            StatusText.Text = $"Report exported: {dialog.FileName}";
            await RecordAuditAsync(new AuditEntry(
                DateTimeOffset.UtcNow,
                AuditAction.ReportExported,
                $"Report exported: {dialog.FileName}",
                SizeInBytes: planItems.Where(item => item.IsEligible).Sum(item => item.SizeInBytes)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusText.Text = $"Report export failed: {ex.Message}";
        }
        finally
        {
            SetCleanupUiEnabled(true);
            ExportCsvButton.IsEnabled = _scanResults.Count > 0;
            ExportHtmlButton.IsEnabled = _scanResults.Count > 0;
            ExportPdfButton.IsEnabled = _scanResults.Count > 0;
        }
    }

    private async void CleanSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        IReadOnlyList<ScanResult> selectedResults = GetSelectedCleanupResults();
        if (selectedResults.Count == 0)
        {
            UpdateCleanSelectedButton();
            return;
        }

        CleanupPolicy policy = new(CleanupMode.MoveToRecycleBin, ExcludedPaths: _excludedPaths.ToArray());
        IReadOnlyList<CleanupPlanItem> planItems = _cleanupPlanner.CreatePlan(selectedResults, policy)
            .Where(item => item.IsEligible)
            .ToArray();

        if (planItems.Count == 0)
        {
            StatusText.Text = "No eligible targets are selected for cleanup.";
            UpdateCleanSelectedButton();
            return;
        }

        if (!ConfirmCleanup(planItems))
        {
            return;
        }

        SetCleanupUiEnabled(false);
        StatusText.Text = string.Empty;
        CurrentScanText.Text = "Cleaning selected targets...";
        ScanActivityText.Text = "Cleaning selected targets";
        CurrentFileText.Text = "Moving selected cache folders to the Recycle Bin.";
        ScanProgressBar.Visibility = Visibility.Visible;
        ScanProgressBar.IsIndeterminate = true;
        StartScanLoader();
        await RecordAuditAsync(new AuditEntry(
            DateTimeOffset.UtcNow,
            AuditAction.CleanupStarted,
            $"Cleanup started for {planItems.Count} target(s).",
            SizeInBytes: planItems.Sum(item => item.SizeInBytes)));

        try
        {
            IReadOnlyList<CleanupResult> results = await _cleanupService.CleanupAsync(
                selectedResults,
                policy,
                CancellationToken.None);

            int succeeded = results.Count(result => result.Succeeded);
            int failed = results.Count - succeeded;
            long reclaimedBytes = results.Sum(result => result.ReclaimedBytes);
            string cleanupStatus = failed == 0
                ? $"Cleanup complete. Moved {succeeded} target(s) to the Recycle Bin. Estimated reclaimed space: {FileSizeFormatter.Format(reclaimedBytes)}."
                : $"Cleanup finished with {failed} failure(s). Moved {succeeded} target(s). Review target permissions and rescan.";

            await RunScanAsync();
            StatusText.Text = cleanupStatus;
            await RecordAuditAsync(new AuditEntry(
                DateTimeOffset.UtcNow,
                AuditAction.CleanupCompleted,
                cleanupStatus,
                SizeInBytes: reclaimedBytes,
                Succeeded: failed == 0,
                ErrorMessage: failed == 0 ? null : cleanupStatus));
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Cleanup canceled.";
        }
        finally
        {
            ScanProgressBar.IsIndeterminate = false;
            ScanProgressBar.Visibility = Visibility.Collapsed;
            StopScanLoader();
            SetCleanupUiEnabled(true);
            UpdateCleanSelectedButton();
        }
    }

    private void AddExclusionButton_Click(object sender, RoutedEventArgs e)
    {
        AddExcludedPath(ExclusionPathTextBox.Text);
        ExclusionPathTextBox.Text = string.Empty;
    }

    private void AddSelectedExclusionButton_Click(object sender, RoutedEventArgs e)
    {
        if (ResultsGrid.SelectedItem is ScanResultRow row)
        {
            AddExcludedPath(row.Path);
        }
    }

    private void RemoveExclusionButton_Click(object sender, RoutedEventArgs e)
    {
        if (ExclusionsList.SelectedItem is not string selectedPath)
        {
            return;
        }

        _excludedPaths.Remove(selectedPath);
        SaveSettings();
        RefreshCleanupPreview();
        RemoveExclusionButton.IsEnabled = false;
        _ = RecordAuditAsync(new AuditEntry(
            DateTimeOffset.UtcNow,
            AuditAction.SettingsChanged,
            $"Exclusion removed: {selectedPath}",
            TargetPath: selectedPath));
    }

    private void ResultsGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        AddSelectedExclusionButton.IsEnabled = ResultsGrid.SelectedItem is ScanResultRow row
            && !string.IsNullOrWhiteSpace(row.Path)
            && row.Path != "-";
    }

    private void ExclusionsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        RemoveExclusionButton.IsEnabled = ExclusionsList.SelectedItem is string;
    }

    private void AddExcludedPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string normalizedPath;
        try
        {
            normalizedPath = Path.GetFullPath(path.Trim())
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            StatusText.Text = "Enter a valid exclusion path.";
            return;
        }

        if (_excludedPaths.Any(existingPath => existingPath.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase)))
        {
            StatusText.Text = "That exclusion is already saved.";
            return;
        }

        _excludedPaths.Add(normalizedPath);
        SaveSettings();
        RefreshCleanupPreview();
        StatusText.Text = string.Empty;
        _ = RecordAuditAsync(new AuditEntry(
            DateTimeOffset.UtcNow,
            AuditAction.SettingsChanged,
            $"Exclusion added: {normalizedPath}",
            TargetPath: normalizedPath));
    }

    private void RefreshCleanupPreview()
    {
        if (_scanResults.Count == 0)
        {
            return;
        }

        CleanupPolicy policy = BuildCleanupPolicy();
        for (int index = 0; index < _scanResults.Count && index < _rows.Count; index++)
        {
            ApplyPlan(_rows[index], _scanResults[index], policy);
        }

        UpdateSummary(_scanResults);
        ResultsGrid.Items.Refresh();
        UpdateCleanSelectedButton();
        CurrentScanText.Text = "Cleanup preview refreshed.";
        ScanActivityText.Text = "Dry-run preview ready";
        CurrentFileText.Text = $"{_excludedPaths.Count} exclusion(s) applied.";
        _ = RecordAuditAsync(new AuditEntry(
            DateTimeOffset.UtcNow,
            AuditAction.CleanupPreviewed,
            $"Cleanup preview refreshed for {_scanResults.Count} target(s)."));
    }

    private CleanupPolicy BuildCleanupPolicy()
    {
        return new CleanupPolicy(ExcludedPaths: _excludedPaths.ToArray());
    }

    private IReadOnlyList<ScanResult> GetSelectedCleanupResults()
    {
        return _rows
            .Select((row, index) => new { Row = row, Index = index })
            .Where(item => item.Row is { IsSelectedForCleanup: true, CanSelectForCleanup: true })
            .Where(item => item.Index < _scanResults.Count)
            .Select(item => _scanResults[item.Index])
            .ToArray();
    }

    private bool ConfirmCleanup(IReadOnlyCollection<CleanupPlanItem> planItems)
    {
        long totalBytes = planItems.Sum(item => item.SizeInBytes);
        StringBuilder message = new();
        message.AppendLine("Move the selected cache targets to the Recycle Bin?");
        message.AppendLine();
        message.AppendLine($"Targets: {planItems.Count}");
        message.AppendLine($"Estimated reclaimable space: {FileSizeFormatter.Format(totalBytes)}");
        message.AppendLine();

        foreach (CleanupPlanItem item in planItems.Take(6))
        {
            message.AppendLine($"{item.Target.DisplayName}: {item.Target.Path}");
        }

        if (planItems.Count > 6)
        {
            message.AppendLine($"...and {planItems.Count - 6} more.");
        }

        message.AppendLine();
        message.AppendLine("This uses the Windows Recycle Bin, but you should still review the selected paths before continuing.");

        MessageBoxResult result = MessageBox.Show(
            this,
            message.ToString(),
            "Confirm cleanup",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        return result is MessageBoxResult.Yes;
    }

    private void CleanupSelection_Changed(object sender, RoutedEventArgs e)
    {
        UpdateCleanSelectedButton();
    }

    private void UpdateCleanSelectedButton()
    {
        CleanSelectedButton.IsEnabled = _rows.Any(row => row.IsSelectedForCleanup && row.CanSelectForCleanup)
            && ScanButton.IsEnabled;
    }

    private void SetCleanupUiEnabled(bool isEnabled)
    {
        ScanButton.IsEnabled = isEnabled;
        PreviewCleanupButton.IsEnabled = isEnabled && _scanResults.Count > 0;
        CleanSelectedButton.IsEnabled = isEnabled;
        ExportCsvButton.IsEnabled = isEnabled && _scanResults.Count > 0;
        ExportHtmlButton.IsEnabled = isEnabled && _scanResults.Count > 0;
        ExportPdfButton.IsEnabled = isEnabled && _scanResults.Count > 0;
        AddExclusionButton.IsEnabled = isEnabled;
        AddSelectedExclusionButton.IsEnabled = isEnabled && ResultsGrid.SelectedItem is ScanResultRow;
        RemoveExclusionButton.IsEnabled = isEnabled && ExclusionsList.SelectedItem is string;
    }

    private async Task RecordAuditAsync(AuditEntry entry)
    {
        try
        {
            await _auditService.RecordAsync(entry, CancellationToken.None);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusText.Text = $"Audit logging skipped: {ex.Message}";
        }
    }

    private void LoadSettings()
    {
        AppSettings settings = _settingsStore.Load();
        foreach (string path in settings.ExcludedPaths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            if (!_excludedPaths.Any(existingPath => existingPath.Equals(path, StringComparison.OrdinalIgnoreCase)))
            {
                _excludedPaths.Add(path);
            }
        }
    }

    private void SaveSettings()
    {
        _settingsStore.Save(new AppSettings
        {
            ExcludedPaths = _excludedPaths.ToList()
        });
    }

    private void SetLastScan(DateTime completedAt)
    {
        string lastScan = completedAt.ToString("dd MMM yyyy, h:mm tt", CultureInfo.CurrentCulture);
        LastScanHeaderText.Text = lastScan;
        LastScanMetricText.Text = completedAt.ToString("h:mm tt", CultureInfo.CurrentCulture);
    }

    private void StartScanLoader()
    {
        ScanLoaderIcon.Visibility = Visibility.Visible;
        DoubleAnimation animation = new()
        {
            From = 0,
            To = 360,
            Duration = TimeSpan.FromSeconds(1),
            RepeatBehavior = RepeatBehavior.Forever
        };
        ScanLoaderRotateTransform.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, animation);
    }

    private void StopScanLoader()
    {
        ScanLoaderRotateTransform.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, null);
        ScanLoaderRotateTransform.Angle = 0;
        ScanLoaderIcon.Visibility = Visibility.Collapsed;
    }

    protected override void OnClosed(EventArgs e)
    {
        _serviceProvider.Dispose();
        base.OnClosed(e);
    }

    private sealed class ScanResultRow
    {
        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;

        public string Exists { get; set; } = string.Empty;

        public string Size { get; set; } = string.Empty;

        public string Risk { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string Recommendation { get; set; } = string.Empty;

        public bool CanSelectForCleanup { get; set; }

        public bool IsSelectedForCleanup { get; set; }
    }
}
