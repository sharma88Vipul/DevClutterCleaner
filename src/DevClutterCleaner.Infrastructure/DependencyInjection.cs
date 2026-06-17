using DevClutterCleaner.Application.Abstractions;
using DevClutterCleaner.Application.Services;
using DevClutterCleaner.Infrastructure.FileSystem;
using DevClutterCleaner.Infrastructure.Scanners;
using Microsoft.Extensions.DependencyInjection;

namespace DevClutterCleaner.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDevClutterCleanerInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IDirectorySizeCalculator, DirectorySizeCalculator>();
        services.AddSingleton<ICacheScanner, NuGetCacheScanner>();
        services.AddSingleton<ICacheScanner, NpmCacheScanner>();
        services.AddSingleton<ICacheScanner, WindowsTempScanner>();
        services.AddSingleton<ICacheScanOrchestrator, CacheScanOrchestrator>();
        services.AddSingleton<IScanService, ScanService>();
        services.AddSingleton<ICleanupPlanner, CleanupPlanner>();
        services.AddSingleton<ICleanupExecutor, RecycleBinCleanupExecutor>();
        services.AddSingleton<ICleanupService, CleanupService>();

        return services;
    }
}
