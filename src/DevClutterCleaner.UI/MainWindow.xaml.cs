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
    private readonly ICacheScanOrchestrator _scanOrchestrator;

    public MainWindow()
    {
        InitializeComponent();

        _serviceProvider = new ServiceCollection()
            .AddDevClutterCleanerInfrastructure()
            .BuildServiceProvider();
        _scanOrchestrator = _serviceProvider.GetRequiredService<ICacheScanOrchestrator>();
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        ScanButton.IsEnabled = false;
        StatusText.Text = string.Empty;

        try
        {
            IReadOnlyList<ScanResult> results = await _scanOrchestrator.ScanAllAsync(CancellationToken.None);
            ResultsList.ItemsSource = results
                .OrderBy(result => result.Target.DisplayName)
                .Select(CreateRow)
                .ToArray();

            long totalBytes = results.Where(result => result.Exists && string.IsNullOrWhiteSpace(result.ErrorMessage))
                .Sum(result => result.SizeInBytes);
            TotalReclaimableResultText.Text = $"Total reclaimable space: {FileSizeFormatter.Format(totalBytes)}";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Scan canceled.";
        }
        finally
        {
            ScanButton.IsEnabled = true;
        }
    }

    private static ScanResultRow CreateRow(ScanResult result)
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

        return new ScanResultRow(
            result.Target.DisplayName,
            result.Target.Path,
            result.Exists ? "Yes" : "No",
            FileSizeFormatter.Format(result.SizeInBytes),
            status);
    }

    protected override void OnClosed(EventArgs e)
    {
        _serviceProvider.Dispose();
        base.OnClosed(e);
    }

    private sealed record ScanResultRow(string Name, string Path, string Exists, string Size, string Status);
}
