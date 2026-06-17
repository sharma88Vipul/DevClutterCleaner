using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media.Animation;
using DevClutterCleaner.Application.Abstractions;
using DevClutterCleaner.Application.Formatting;
using DevClutterCleaner.Domain;
using DevClutterCleaner.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DevClutterCleaner.UI;

public partial class MainWindow : Window
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IReadOnlyList<ICacheScanner> _scanners;
    private readonly ICleanupPlanner _cleanupPlanner;
    private readonly ObservableCollection<ScanResultRow> _rows = [];

    public MainWindow()
    {
        InitializeComponent();

        _serviceProvider = new ServiceCollection()
            .AddDevClutterCleanerInfrastructure()
            .BuildServiceProvider();
        _scanners = _serviceProvider.GetServices<ICacheScanner>().ToArray();
        _cleanupPlanner = _serviceProvider.GetRequiredService<ICleanupPlanner>();
        ResultsGrid.ItemsSource = _rows;
        ScanCountText.Text = $"0 / {_scanners.Count}";
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        ScanButton.IsEnabled = false;
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
        _rows.Clear();
        StartScanLoader();

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
                    ApplyResult(row, result);
                    ApplyPlan(row, result);
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
            SetLastScan(DateTime.Now);
            CurrentScanText.Text = "Scan complete.";
            ScanActivityText.Text = "Scan complete";
            CurrentFileText.Text = "All configured cache targets were scanned.";
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
        CleanupPlanItem planItem = _cleanupPlanner.CreatePlan([result], new CleanupPolicy()).Single();

        row.Risk = planItem.Risk.ToString();
        row.Recommendation = planItem.IsEligible
            ? "Safe candidate for dry-run cleanup planning."
            : planItem.Reason;
    }

    private void UpdateSummary(IReadOnlyCollection<ScanResult> results)
    {
        long totalBytes = results
            .Where(result => result.Exists && string.IsNullOrWhiteSpace(result.ErrorMessage))
            .Sum(result => result.SizeInBytes);
        IReadOnlyList<CleanupPlanItem> planItems = _cleanupPlanner.CreatePlan(results, new CleanupPolicy());
        int lowRiskCount = planItems.Count(item => item.Risk is RiskClassification.Low && item.IsEligible);
        int needsReviewCount = planItems.Count(item => !item.IsEligible || item.Risk is RiskClassification.High or RiskClassification.Blocked);

        string total = FileSizeFormatter.Format(totalBytes);
        TotalReclaimableMetricText.Text = total;
        TargetsScannedMetricText.Text = results.Count.ToString();
        LowRiskMetricText.Text = lowRiskCount.ToString();
        NeedsReviewMetricText.Text = needsReviewCount.ToString();
        TotalReclaimableResultText.Text = $"Total reclaimable space: {total}";
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
    }
}
