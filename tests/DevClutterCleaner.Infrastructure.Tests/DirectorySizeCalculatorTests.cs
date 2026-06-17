using DevClutterCleaner.Infrastructure.FileSystem;

namespace DevClutterCleaner.Infrastructure.Tests;

public sealed class DirectorySizeCalculatorTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"dcc-{Guid.NewGuid():N}");
    private readonly DirectorySizeCalculator _calculator = new();

    public DirectorySizeCalculatorTests()
    {
        Directory.CreateDirectory(_rootPath);
    }

    [Fact]
    public void CalculateSize_ReturnsTotalSizeIncludingNestedFiles()
    {
        string childPath = Path.Combine(_rootPath, "package");
        Directory.CreateDirectory(childPath);
        File.WriteAllBytes(Path.Combine(_rootPath, "root.bin"), new byte[10]);
        File.WriteAllBytes(Path.Combine(childPath, "child.bin"), new byte[15]);

        long size = _calculator.CalculateSize(_rootPath, CancellationToken.None);

        Assert.Equal(25, size);
    }

    [Fact]
    public void CalculateSize_ReturnsZero_WhenDirectoryDoesNotExist()
    {
        string missingPath = Path.Combine(_rootPath, "missing");

        long size = _calculator.CalculateSize(missingPath, CancellationToken.None);

        Assert.Equal(0, size);
    }

    [Fact]
    public void CalculateSize_Throws_WhenCancellationIsRequested()
    {
        using CancellationTokenSource cts = new();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() => _calculator.CalculateSize(_rootPath, cts.Token));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
