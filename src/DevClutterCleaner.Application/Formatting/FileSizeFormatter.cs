using System.Globalization;

namespace DevClutterCleaner.Application.Formatting;

public static class FileSizeFormatter
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB"];

    public static string Format(long bytes)
    {
        if (bytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bytes), "File size cannot be negative.");
        }

        double size = bytes;
        int unitIndex = 0;

        while (size >= 1024 && unitIndex < Units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        string format = unitIndex == 0 ? "0" : "0.##";
        return $"{size.ToString(format, CultureInfo.InvariantCulture)} {Units[unitIndex]}";
    }
}
