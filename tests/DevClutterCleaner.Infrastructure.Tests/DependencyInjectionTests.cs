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
        Assert.Contains(scanners, scanner => scanner is WindowsTempScanner);
        Assert.NotNull(serviceProvider.GetRequiredService<ICacheScanOrchestrator>());
    }
}
