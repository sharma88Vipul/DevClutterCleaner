using DevClutterCleaner.Application.Abstractions;
using DevClutterCleaner.Infrastructure.Scanners;
using Microsoft.Extensions.DependencyInjection;

namespace DevClutterCleaner.Infrastructure.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddDevClutterCleanerInfrastructure_RegistersScannersAndOrchestrator()
    {
        using ServiceProvider serviceProvider = new ServiceCollection()
            .AddDevClutterCleanerInfrastructure()
            .BuildServiceProvider();

        var scanners = serviceProvider.GetServices<ICacheScanner>().ToArray();

        Assert.Contains(scanners, scanner => scanner is NuGetCacheScanner);
        Assert.Contains(scanners, scanner => scanner is NpmCacheScanner);
        Assert.Contains(scanners, scanner => scanner is PnpmCacheScanner);
        Assert.Contains(scanners, scanner => scanner is PipCacheScanner);
        Assert.Contains(scanners, scanner => scanner is WindowsTempScanner);
        Assert.Contains(scanners, scanner => scanner is VsCodeCacheScanner);
        Assert.Contains(scanners, scanner => scanner is DockerCacheScanner);
        Assert.Contains(scanners, scanner => scanner is GitCacheScanner);
        Assert.Contains(scanners, scanner => scanner is ClaudeCliCacheScanner);
        Assert.Contains(scanners, scanner => scanner is CodexCliCacheScanner);
        Assert.NotNull(serviceProvider.GetRequiredService<ICacheScanOrchestrator>());
        Assert.NotNull(serviceProvider.GetRequiredService<ICleanupPlanner>());
        Assert.NotNull(serviceProvider.GetRequiredService<ICleanupService>());
        Assert.NotNull(serviceProvider.GetRequiredService<IReportService>());
        Assert.NotNull(serviceProvider.GetRequiredService<IAuditService>());
        Assert.NotNull(serviceProvider.GetRequiredService<ILicenseService>());
    }
}
