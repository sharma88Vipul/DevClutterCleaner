using DevClutterCleaner.Domain;
using DevClutterCleaner.Infrastructure.FileSystem;
using DevClutterCleaner.Infrastructure.Scanners;

namespace DevClutterCleaner.Infrastructure.Tests;

public sealed class NpmCacheScannerTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"dcc-npm-{Guid.NewGuid():N}");

    public NpmCacheScannerTests()
    {
        Directory.CreateDirectory(_rootPath);
    }

    [Fact]
    public async Task ScanAsync_ReturnsMissingResult_WhenNpmCacheFolderDoesNotExist()
    {
        string missingPath = Path.Combine(_rootPath, "missing-npm-cache");
        NpmCacheScanner scanner = new(new DirectorySizeCalculator(), () => missingPath);

        var result = await scanner.ScanAsync(CancellationToken.None);

        Assert.False(result.Exists);
        Assert.Equal(0, result.SizeInBytes);
        Assert.Equal(missingPath, result.Target.Path);
    }

    [Fact]
    public async Task ScanAsync_ReturnsCacheSize_WhenNpmCacheFolderExists()
    {
        string cachePath = Path.Combine(_rootPath, "npm-cache");
        Directory.CreateDirectory(cachePath);
        File.WriteAllBytes(Path.Combine(cachePath, "package.tgz"), new byte[96]);
        NpmCacheScanner scanner = new(new DirectorySizeCalculator(), () => cachePath);

        var result = await scanner.ScanAsync(CancellationToken.None);

        Assert.True(result.Exists);
        Assert.Equal(96, result.SizeInBytes);
        Assert.Equal("npm Cache", result.Target.DisplayName);
        Assert.Equal(CacheCategory.PackageManager, result.Target.Category);
    }

    [Fact]
    public async Task ScanAsync_UsesDefaultNpmCachePath()
    {
        NpmCacheScanner scanner = new(new StubDirectorySizeCalculator());

        var result = await scanner.ScanAsync(CancellationToken.None);

        string expectedPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "npm-cache");
        Assert.Equal(expectedPath, result.Target.Path);
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
        public long CalculateSize(string path, CancellationToken cancellationToken) => 0;
    }
}
