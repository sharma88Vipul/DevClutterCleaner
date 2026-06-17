using DevClutterCleaner.Application.Formatting;

namespace DevClutterCleaner.Application.Tests;

public sealed class FileSizeFormatterTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(1073741824, "1 GB")]
    [InlineData(5368709120, "5 GB")]
    public void Format_ReturnsHumanReadableSize(long bytes, string expected)
    {
        string result = FileSizeFormatter.Format(bytes);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Format_Throws_WhenSizeIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FileSizeFormatter.Format(-1));
    }
}
