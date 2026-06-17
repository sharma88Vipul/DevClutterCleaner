using DevClutterCleaner.Application.Abstractions;
using DevClutterCleaner.Application.Services;
using DevClutterCleaner.Domain;

namespace DevClutterCleaner.Application.Tests;

public sealed class ScanServiceTests
{
    [Fact]
    public async Task ScanAllAsync_ReturnsResultsFromRegisteredScanners()
    {
        ScanResult expected = new(
            new CacheTarget("test", "Test Cache", CacheCategory.Other, "C:\\temp", false),
            42,
            Exists: true);
        ScanService service = new([new StubScanner(expected)]);

        IReadOnlyList<ScanResult> results = await service.ScanAllAsync(CancellationToken.None);

        ScanResult result = Assert.Single(results);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ScanAllAsync_ReturnsErrorResult_WhenScannerThrows()
    {
        ScanService service = new([new ThrowingScanner()]);

        ScanResult result = Assert.Single(await service.ScanAllAsync(CancellationToken.None));

        Assert.Equal("throwing-scanner", result.Target.Id);
        Assert.False(result.Exists);
        Assert.Equal(0, result.SizeInBytes);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ScanAllAsync_Throws_WhenCancellationIsRequested()
    {
        ScanService service = new([new StubScanner(new ScanResult(
            new CacheTarget("test", "Test Cache", CacheCategory.Other, "C:\\temp", false),
            42,
            Exists: true))]);
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.ScanAllAsync(cts.Token));
    }

    private sealed class StubScanner(ScanResult result) : ICacheScanner
    {
        public string Id => result.Target.Id;

        public CacheCategory TargetType => result.Target.Category;

        public Task<ScanResult> ScanAsync(CancellationToken cancellationToken) => Task.FromResult(result);
    }

    private sealed class ThrowingScanner : ICacheScanner
    {
        public string Id => "throwing-scanner";

        public CacheCategory TargetType => CacheCategory.Other;

        public Task<ScanResult> ScanAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("Scan failed.");
    }
}
