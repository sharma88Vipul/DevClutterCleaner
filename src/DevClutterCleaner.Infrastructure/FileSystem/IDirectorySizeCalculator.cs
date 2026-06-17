namespace DevClutterCleaner.Infrastructure.FileSystem;

public interface IDirectorySizeCalculator
{
    long CalculateSize(
        string path,
        CancellationToken cancellationToken,
        IProgress<string>? progress = null);
}
