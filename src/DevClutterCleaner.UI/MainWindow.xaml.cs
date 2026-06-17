using System.Collections.ObjectModel;
using System.Windows;
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
    private readonly ObservableCollection<ScanResultRow> _rows = [];

    public MainWindow()
    {
        InitializeComponent();

        _serviceProvider = new ServiceCollection()
            .AddDevClutterCleanerInfrastructure()
            .BuildServiceProvider();
        _scanners = _serviceProvider.GetServices<ICacheScanner>().ToArray();
        ResultsGrid.ItemsSource = _rows;
        ScanCountText.Text = $"0 / {_scanners.Count}";
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        ScanButton.IsEnabled = false;
        ScanButtonText.Text = "Scanning";
        StatusText.Text = string.Empty;
        CurrentScanText.Text = "Preparing scanner targets...";
        ScanProgressBar.Visibility = Visibility.Visible;
        ScanProgressBar.IsIndeterminate = true;
        TotalReclaimableResultText.Text = "Total reclaimable space: Scanning...";
        _rows.Clear();

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
                ScanCountText.Text = $"{index} / {_scanners.Count}";
                ResultsGrid.Items.Refresh();

                await Task.Yield();

                try
                {
                    ScanResult result = await scanner.ScanAsync(CancellationToken.None);
                    results.Add(result);
                    ApplyResult(row, result);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    row.Status = ex.Message;
                    row.Exists = "No";
                    row.Size = "0 B";
                }

                ScanCountText.Text = $"{index + 1} / {_scanners.Count}";
                ResultsGrid.Items.Refresh();
            }

            long totalBytes = results
                .Where(result => result.Exists && string.IsNullOrWhiteSpace(result.ErrorMessage))
                .Sum(result => result.SizeInBytes);
            TotalReclaimableResultText.Text = $"Total reclaimable space: {FileSizeFormatter.Format(totalBytes)}";
            CurrentScanText.Text = "Scan complete.";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Scan canceled.";
            CurrentScanText.Text = "Scan canceled.";
        }
        finally
        {
            ScanProgressBar.IsIndeterminate = false;
            ScanProgressBar.Visibility = Visibility.Collapsed;
            ScanButtonText.Text = "Scan caches";
            ScanButton.IsEnabled = true;
        }
    }

    private static ScanResultRow CreatePendingRow(ICacheScanner scanner)
    {
        return new ScanResultRow
        {
            Name = scanner.TargetDisplayName,
            Path = scanner.GetTargetPath(),
            Exists = "-",
            Size = "-",
            Status = "Pending"
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
        row.Path = result.Target.Path;
        row.Exists = result.Exists ? "Yes" : "No";
        row.Size = FileSizeFormatter.Format(result.SizeInBytes);
        row.Status = status;
    }

    protected override void OnClosed(EventArgs e)
    {
        _serviceProvider.Dispose();
        base.OnClosed(e);
    }

    private sealed class ScanResultRow
    {
        public string Name { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;

        public string Exists { get; set; } = string.Empty;

        public string Size { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }
}
