using DevClutterCleaner.Application.Abstractions;
using DevClutterCleaner.Application.Services;
using DevClutterCleaner.Domain;

namespace DevClutterCleaner.Application.Tests;

public sealed class ScanServiceTests
{
    [Fact]
    public void ScanAll_ReturnsResultsFromRegisteredScanners()
    {
        ScanResult expected = new(
            new CacheTarget("test", "Test Cache", CacheCategory.Other, "C:\\temp", false),
            42,
            Exists: true);
        ScanService service = new([new StubScanner(expected)]);

        IReadOnlyList<ScanResult> results = service.ScanAll();

        ScanResult result = Assert.Single(results);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ScanAll_ReturnsErrorResult_WhenScannerThrows()
    {
        ScanService service = new([new ThrowingScanner()]);

        ScanResult result = Assert.Single(service.ScanAll());

        Assert.Equal("throwing-scanner", result.Target.Id);
        Assert.False(result.Exists);
        Assert.Equal(0, result.SizeInBytes);
        Assert.NotNull(result.ErrorMessage);
    }

    private sealed class StubScanner(ScanResult result) : ICacheScanner
    {
        public string Id => result.Target.Id;

        public ScanResult Scan() => result;
    }

    private sealed class ThrowingScanner : ICacheScanner
    {
        public string Id => "throwing-scanner";

        public ScanResult Scan() => throw new InvalidOperationException("Scan failed.");
    }
}
