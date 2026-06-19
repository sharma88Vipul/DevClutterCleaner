using DevClutterCleaner.Infrastructure.Logging;

namespace DevClutterCleaner.UI;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        DevClutterLogger.Configure();
        base.OnStartup(e);
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        DevClutterLogger.Shutdown();
        base.OnExit(e);
    }
}
