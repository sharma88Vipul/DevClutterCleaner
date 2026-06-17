using DevClutterCleaner.Infrastructure.FileSystem;
using DevClutterCleaner.Infrastructure.Scanners;
using DevClutterCleaner.Domain;

namespace DevClutterCleaner.Infrastructure.Tests;

public sealed class NuGetCacheScannerTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"dcc-nuget-{Guid.NewGuid():N}");

    public NuGetCacheScannerTests()
    {
        Directory.CreateDirectory(_rootPath);
    }

    [Fact]
    public async Task ScanAsync_ReturnsMissingResult_WhenNuGetCacheFolderDoesNotExist()
    {
        string missingPath = Path.Combine(_rootPath, "missing-packages");
        NuGetCacheScanner scanner = new(new DirectorySizeCalculator(), () => missingPath);

        var result = await scanner.ScanAsync(CancellationToken.None);

        Assert.False(result.Exists);
        Assert.Equal(0, result.SizeInBytes);
        Assert.Equal(missingPath, result.Target.Path);
    }

    [Fact]
    public async Task ScanAsync_ReturnsCacheSize_WhenNuGetCacheFolderExists()
    {
        string packagesPath = Path.Combine(_rootPath, "packages");
        Directory.CreateDirectory(packagesPath);
        File.WriteAllBytes(Path.Combine(packagesPath, "package.nupkg"), new byte[64]);
        NuGetCacheScanner scanner = new(new DirectorySizeCalculator(), () => packagesPath);

        var result = await scanner.ScanAsync(CancellationToken.None);

        Assert.True(result.Exists);
        Assert.Equal(64, result.SizeInBytes);
        Assert.Equal("NuGet Cache", result.Target.DisplayName);
        Assert.Equal(CacheCategory.PackageManager, result.Target.Category);
        Assert.True(result.Target.IsSafeToCleanByDefault);
    }

    [Fact]
    public async Task ScanAsync_UsesDefaultNuGetPackagesPath()
    {
        NuGetCacheScanner scanner = new(new StubDirectorySizeCalculator());

        var result = await scanner.ScanAsync(CancellationToken.None);

        string expectedPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget",
            "packages");
        Assert.Equal(expectedPath, result.Target.Path);
    }

    [Fact]
    public async Task ScanAsync_Throws_WhenCancellationIsRequested()
    {
        NuGetCacheScanner scanner = new(new StubDirectorySizeCalculator());
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => scanner.ScanAsync(cts.Token));
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
