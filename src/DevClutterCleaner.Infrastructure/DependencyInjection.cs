using DevClutterCleaner.Application.Abstractions;
using DevClutterCleaner.Application.Services;
using DevClutterCleaner.Infrastructure.Audit;
using DevClutterCleaner.Infrastructure.FileSystem;
using DevClutterCleaner.Infrastructure.Licensing;
using DevClutterCleaner.Infrastructure.Plugins;
using DevClutterCleaner.Infrastructure.Reports;
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
        services.AddSingleton<ICacheScanner, PnpmCacheScanner>();
        services.AddSingleton<ICacheScanner, PipCacheScanner>();
        services.AddSingleton<ICacheScanner, WindowsTempScanner>();
        services.AddSingleton<ICacheScanner, VsCodeCacheScanner>();
        services.AddSingleton<ICacheScanner, DockerCacheScanner>();
        services.AddSingleton<ICacheScanner, GitCacheScanner>();
        services.AddSingleton<ICacheScanner, ClaudeCliCacheScanner>();
        services.AddSingleton<ICacheScanner, CodexCliCacheScanner>();
        services.AddSingleton<ICacheScannerPlugin, BuiltInCacheScannerPlugin>();
        services.AddSingleton<PluginCacheScannerProvider>();
        services.AddSingleton<ICacheScanOrchestrator, CacheScanOrchestrator>();
        services.AddSingleton<IScanService, ScanService>();
        services.AddSingleton<ICleanupPlanner, CleanupPlanner>();
        services.AddSingleton<ICleanupExecutor, RecycleBinCleanupExecutor>();
        services.AddSingleton<ICleanupService, CleanupService>();
        services.AddSingleton<IReportService, ReportService>();
        services.AddSingleton<IAuditService, FileAuditService>();
        services.AddSingleton<ILicenseService, CommunityLicenseService>();

        return services;
    }
}
