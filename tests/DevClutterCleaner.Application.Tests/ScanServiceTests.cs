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
    public async Task ScanAllAsync_PreservesOrderAndContinuesAfterScannerFailure()
    {
        ScanResult first = new(
            new CacheTarget("first", "First", CacheCategory.Other, "C:\\first", false),
            1,
            Exists: true);
        ScanResult last = new(
            new CacheTarget("last", "Last", CacheCategory.Other, "C:\\last", false),
            3,
            Exists: true);
        ScanService service = new([new StubScanner(first), new ThrowingScanner(), new StubScanner(last)]);

        IReadOnlyList<ScanResult> results = await service.ScanAllAsync(CancellationToken.None);

        Assert.Collection(
            results,
            result => Assert.Equal("first", result.Target.Id),
            result =>
            {
                Assert.Equal("throwing-scanner", result.Target.Id);
                Assert.NotNull(result.ErrorMessage);
            },
            result => Assert.Equal("last", result.Target.Id));
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

        public string TargetDisplayName => result.Target.DisplayName;

        public CacheCategory TargetType => result.Target.Category;

        public string GetTargetPath() => result.Target.Path;

        public Task<ScanResult> ScanAsync(
            CancellationToken cancellationToken,
            IProgress<ScanProgress>? progress = null) => Task.FromResult(result);
    }

    private sealed class ThrowingScanner : ICacheScanner
    {
        public string Id => "throwing-scanner";

        public string TargetDisplayName => "Throwing Scanner";

        public CacheCategory TargetType => CacheCategory.Other;

        public string GetTargetPath() => string.Empty;

        public Task<ScanResult> ScanAsync(
            CancellationToken cancellationToken,
            IProgress<ScanProgress>? progress = null) => throw new InvalidOperationException("Scan failed.");
    }
}
