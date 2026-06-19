using DevClutterCleaner.Domain;
using DevClutterCleaner.Infrastructure.FileSystem;
using DevClutterCleaner.Infrastructure.Scanners;

namespace DevClutterCleaner.Infrastructure.Tests;

public sealed class NewScannerTests
{
    [Fact]
    public async Task CodexCliCacheScanner_ReturnsAiToolingTarget()
    {
        string directory = Directory.CreateTempSubdirectory().FullName;
        await File.WriteAllTextAsync(Path.Combine(directory, "session.json"), "test");
        CodexCliCacheScanner scanner = new(new DirectorySizeCalculator(), () => directory);

        ScanResult result = await scanner.ScanAsync(CancellationToken.None);

        Assert.True(result.Exists);
        Assert.Equal(CacheCategory.AiTooling, result.Target.Category);
        Assert.Equal(CodexCliCacheScanner.ScannerId, result.Target.Id);
        Assert.False(result.Target.IsSafeToCleanByDefault);
        Assert.True(result.SizeInBytes > 0);
    }
}
