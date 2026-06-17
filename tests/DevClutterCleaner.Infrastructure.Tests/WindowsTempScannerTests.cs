using DevClutterCleaner.Domain;
using DevClutterCleaner.Infrastructure.FileSystem;
using DevClutterCleaner.Infrastructure.Scanners;

namespace DevClutterCleaner.Infrastructure.Tests;

public sealed class WindowsTempScannerTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"dcc-temp-{Guid.NewGuid():N}");

    public WindowsTempScannerTests()
    {
        Directory.CreateDirectory(_rootPath);
    }

    [Fact]
    public async Task ScanAsync_ReturnsTempFolderSize()
    {
        File.WriteAllBytes(Path.Combine(_rootPath, "temp.bin"), new byte[128]);
        WindowsTempScanner scanner = new(new DirectorySizeCalculator(), () => _rootPath);

        var result = await scanner.ScanAsync(CancellationToken.None);

        Assert.True(result.Exists);
        Assert.Equal(128, result.SizeInBytes);
        Assert.Equal("Windows Temp", result.Target.DisplayName);
        Assert.Equal(CacheCategory.TemporaryFiles, result.Target.Category);
    }

    [Fact]
    public async Task ScanAsync_ReturnsMissingResult_WhenTempFolderDoesNotExist()
    {
        string missingPath = Path.Combine(_rootPath, "missing-temp");
        WindowsTempScanner scanner = new(new DirectorySizeCalculator(), () => missingPath);

        var result = await scanner.ScanAsync(CancellationToken.None);

        Assert.False(result.Exists);
        Assert.Equal(0, result.SizeInBytes);
        Assert.Equal(missingPath, result.Target.Path);
    }

    [Fact]
    public async Task ScanAsync_UsesPathGetTempPathByDefault()
    {
        WindowsTempScanner scanner = new(new StubDirectorySizeCalculator());

        var result = await scanner.ScanAsync(CancellationToken.None);

        Assert.Equal(Path.GetTempPath(), result.Target.Path);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }

    private sealed class StubDirectorySizeCalculator : IDirectorySizeCalculator
    {
        public long CalculateSize(
            string path,
            CancellationToken cancellationToken,
            IProgress<string>? progress = null) => 0;
    }
}
