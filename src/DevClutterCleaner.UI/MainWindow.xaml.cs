using System.Windows;
using DevClutterCleaner.Application.Abstractions;
using DevClutterCleaner.Application.Formatting;
using DevClutterCleaner.Application.Services;
using DevClutterCleaner.Domain;
using DevClutterCleaner.Infrastructure.FileSystem;
using DevClutterCleaner.Infrastructure.Scanners;

namespace DevClutterCleaner.UI;

public partial class MainWindow : Window
{
    private readonly IScanService _scanService;

    public MainWindow()
    {
        InitializeComponent();

        DirectorySizeCalculator directorySizeCalculator = new();
        _scanService = new ScanService([
            new NuGetCacheScanner(directorySizeCalculator)
        ]);
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        IReadOnlyList<ScanResult> results = await _scanService.ScanAllAsync(CancellationToken.None);
        ScanResult? nugetResult = results.FirstOrDefault(result => result.Target.Id == NuGetCacheScanner.ScannerId);

        if (nugetResult is null)
        {
            NuGetCacheResultText.Text = "NuGet Cache: Not available";
            TotalReclaimableResultText.Text = "Total reclaimable space: 0 B";
            StatusText.Text = "NuGet scanner was not registered.";
            return;
        }

        string size = FileSizeFormatter.Format(nugetResult.SizeInBytes);
        NuGetCacheResultText.Text = $"NuGet Cache: {size}";
        TotalReclaimableResultText.Text = $"Total reclaimable space: {size}";
        StatusText.Text = GetStatusMessage(nugetResult);
    }

    private static string GetStatusMessage(ScanResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            return result.ErrorMessage;
        }

        return result.Exists
            ? $"Scanned: {result.Target.Path}"
            : $"Folder not found: {result.Target.Path}";
    }
}
