# DevClutterCleaner

DevClutterCleaner is a Windows desktop utility for developers who want to see how much disk space is being used by common development caches before deciding what to clean up.

The current MVP is scan-only. It reports cache locations, whether each location exists, and the estimated disk usage. It does not delete files or run cleanup commands.

## Problem

Developer machines collect package caches, temporary files, and tool artifacts over time. These folders are useful, but they can quietly grow large and make it hard to understand where disk space went.

DevClutterCleaner gives a small, read-only dashboard for checking common developer cache locations without requiring users to manually inspect folders.

## MVP Features

- WPF dashboard with a single `Scan caches` action.
- Async scan orchestration across registered cache scanners.
- Per-target results for path, existence, formatted size, and status.
- Total reclaimable size summary.
- Safe recursive directory size calculation that skips inaccessible files and folders where possible.
- Unit tests for formatting, scanner behavior, missing folders, directory size calculation, DI registration, and scan orchestration.

## Supported Cache Targets

| Target | Default path | Status |
| --- | --- | --- |
| NuGet global packages | `%USERPROFILE%\.nuget\packages` | Implemented |
| npm cache | `%LOCALAPPDATA%\npm-cache` | Implemented |
| Windows Temp | Current user temp path from `Path.GetTempPath()` | Implemented |

## Screenshot

Screenshot placeholder: add an image of the WPF scan dashboard after the UI is finalized.

## Safety

This version only scans and reports size. It does not delete files, clear caches, run package-manager cleanup commands, install services, collect telemetry, or modify system folders.

Scanner behavior is intentionally conservative:

- Missing folders return `Exists = false` and `SizeInBytes = 0`.
- Locked or inaccessible files are skipped where possible.
- Cancellation tokens are supported through the scan contracts.

## Solution Structure

```text
src/
  DevClutterCleaner.Domain
  DevClutterCleaner.Application
  DevClutterCleaner.Infrastructure
  DevClutterCleaner.UI
tests/
  DevClutterCleaner.Application.Tests
  DevClutterCleaner.Infrastructure.Tests
```

## Requirements

- Windows
- .NET 10 SDK
- Visual Studio with the .NET desktop development workload for WPF development

## Build and Test

```powershell
dotnet restore
dotnet build
dotnet test
```

## Run the UI

```powershell
dotnet run --project src/DevClutterCleaner.UI/DevClutterCleaner.UI.csproj
```

Click `Scan caches` to scan NuGet, npm, and Windows Temp cache locations and display individual and total disk usage.

## Roadmap

- Add more developer cache scanners, including Codex CLI, Claude CLI, IDE caches, and container tooling.
- Improve result sorting, filtering, and scan progress feedback.
- Add optional cleanup planning with clear previews and confirmations.
- Keep destructive cleanup disabled until scanner behavior is stable and well tested.
- Add installer and release packaging after the scanner MVP is complete.
