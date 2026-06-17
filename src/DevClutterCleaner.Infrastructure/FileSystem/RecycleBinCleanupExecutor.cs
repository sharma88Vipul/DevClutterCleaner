using DevClutterCleaner.Application.Abstractions;
using DevClutterCleaner.Domain;
using Microsoft.VisualBasic.FileIO;
using VisualBasicFileSystem = Microsoft.VisualBasic.FileIO.FileSystem;

namespace DevClutterCleaner.Infrastructure.FileSystem;

public sealed class RecycleBinCleanupExecutor : ICleanupExecutor
{
    public Task CleanupAsync(CleanupPlanItem planItem, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(planItem);

        string path = planItem.Target.Path;
        if (!Directory.Exists(path))
        {
            return Task.CompletedTask;
        }

        VisualBasicFileSystem.DeleteDirectory(
            path,
            UIOption.OnlyErrorDialogs,
            RecycleOption.SendToRecycleBin);

        return Task.CompletedTask;
    }
}
