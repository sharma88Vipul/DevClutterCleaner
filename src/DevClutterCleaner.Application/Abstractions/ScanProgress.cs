namespace DevClutterCleaner.Application.Abstractions;

public sealed record ScanProgress(
    string TargetName,
    string CurrentPath);
