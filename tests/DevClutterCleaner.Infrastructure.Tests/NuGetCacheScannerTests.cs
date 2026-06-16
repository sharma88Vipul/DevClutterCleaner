using DevClutterCleaner.Infrastructure.FileSystem;
using DevClutterCleaner.Infrastructure.Scanners;

namespace DevClutterCleaner.Infrastructure.Tests;

public sealed class NuGetCacheScannerTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"dcc-nuget-{Guid.NewGuid():N}");

    public NuGetCacheScannerTests()
    {
        Directory.CreateDirectory(_rootPath);
    }

    [Fact]
    public void Scan_ReturnsMissingResult_WhenNuGetCacheFolderDoesNotExist()
    {
        string missingPath = Path.Combine(_rootPath, "missing-packages");
        NuGetCacheScanner scanner = new(new DirectorySizeCalculator(), () => missingPath);

        var result = scanner.Scan();

        Assert.False(result.Exists);
        Assert.Equal(0, result.SizeInBytes);
        Assert.Equal(missingPath, result.Target.Path);
    }

    [Fact]
    public void Scan_ReturnsCacheSize_WhenNuGetCacheFolderExists()
    {
        string packagesPath = Path.Combine(_rootPath, "packages");
        Directory.CreateDirectory(packagesPath);
        File.WriteAllBytes(Path.Combine(packagesPath, "package.nupkg"), new byte[64]);
        NuGetCacheScanner scanner = new(new DirectorySizeCalculator(), () => packagesPath);

        var result = scanner.Scan();

        Assert.True(result.Exists);
        Assert.Equal(64, result.SizeInBytes);
        Assert.Equal("NuGet Cache", result.Target.DisplayName);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
