using Serilog;

namespace DevClutterCleaner.Infrastructure.Logging;

public static class DevClutterLogger
{
    public static void Configure()
    {
        string logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DevClutterCleaner",
            "logs");

        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.WithProperty("Application", "DevClutterCleaner")
            .WriteTo.File(
                Path.Combine(logDirectory, "devcluttercleaner-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14)
            .CreateLogger();

        Log.Information("DevClutterCleaner logging initialized.");
    }

    public static void Shutdown()
    {
        Log.Information("DevClutterCleaner shutting down.");
        Log.CloseAndFlush();
    }
}
