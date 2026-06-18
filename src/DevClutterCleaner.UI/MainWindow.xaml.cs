using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
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
        ResultsGrid.ItemsSource = _rows;
        ExclusionsList.ItemsSource = _excludedPaths;
        LoadSettings();
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
        PreviewCleanupButton.IsEnabled = false;
        AddSelectedExclusionButton.IsEnabled = false;
        _rows.Clear();
        _scanResults.Clear();
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
        ApplyPlan(row, result, BuildCleanupPolicy());
    }

    private void ApplyPlan(ScanResultRow row, ScanResult result, CleanupPolicy policy)
    {
        CleanupPlanItem planItem = _cleanupPlanner.CreatePlan([result], policy).Single();

        row.Risk = planItem.Risk.ToString();
        row.Recommendation = planItem.IsEligible
            ? $"Dry-run eligible: {FileSizeFormatter.Format(planItem.SizeInBytes)}."
            : planItem.Reason;
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
        CurrentScanText.Text = "Cleanup preview refreshed.";
        ScanActivityText.Text = "Dry-run preview ready";
        CurrentFileText.Text = $"{_excludedPaths.Count} exclusion(s) applied.";
    }

    private CleanupPolicy BuildCleanupPolicy()
    {
        return new CleanupPolicy(ExcludedPaths: _excludedPaths.ToArray());
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
    }
}
