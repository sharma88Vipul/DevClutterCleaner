# DevClutterCleaner

DevClutterCleaner is a Windows desktop utility for developers who want to see how much disk space is being used by common development caches before deciding what to clean up.

The current MVP scans developer cache locations, previews cleanup eligibility, supports saved exclusions, exports reports, and can move explicitly selected low-risk targets to the Windows Recycle Bin after confirmation.

## Problem

Developer machines collect package caches, temporary files, and tool artifacts over time. These folders are useful, but they can quietly grow large and make it hard to understand where disk space went.

DevClutterCleaner gives a small dashboard for checking common developer cache locations without requiring users to manually inspect folders, then keeps cleanup conservative through risk classification, exclusions, confirmation, and Recycle Bin behavior.

## MVP Features

- WPF dashboard with scan, preview cleanup, selected cleanup, exclusions, and report export actions.
- Async scan orchestration across registered cache scanners.
- Per-target results for path, existence, formatted size, and status.
- Total reclaimable size summary.
- Safe recursive directory size calculation that skips inaccessible files and folders where possible.
- Cleanup planning with low/high/blocked risk classification.
- Saved path exclusions.
- Explicit selected cleanup through Windows Recycle Bin.
- CSV, HTML, and PDF report export.
- Local JSON-lines audit log.
- Built-in plugin registration seam for cache scanners.
- CI build/test/publish workflow and MSIX packaging checklist.
- Unit tests for formatting, scanner behavior, missing folders, directory size calculation, DI registration, reporting, audit logging, cleanup planning, and scan orchestration.

## Supported Cache Targets

| Target | Default path | Status |
| --- | --- | --- |
| NuGet global packages | `%USERPROFILE%\.nuget\packages` | Implemented |
| npm cache | `%LOCALAPPDATA%\npm-cache` | Implemented |
| pnpm store | `%LOCALAPPDATA%\pnpm\store` | Implemented |
| pip cache | `%LOCALAPPDATA%\pip\Cache` | Implemented |
| Windows Temp | Current user temp path from `Path.GetTempPath()` | Implemented |
| VS Code cache | `%APPDATA%\Code\Cache` | Implemented |
| Docker Desktop data | `%LOCALAPPDATA%\Docker` | Implemented, high-risk |
| Git Credential Manager cache | `%LOCALAPPDATA%\GitCredentialManager` | Implemented, high-risk |
| Claude CLI cache | `%USERPROFILE%\.claude` | Implemented, high-risk |
| Codex CLI cache | `%USERPROFILE%\.codex` | Implemented, high-risk |

## Screenshot

Screenshot placeholder: add an image of the WPF scan dashboard after the UI is finalized.

## Safety

Cleanup is opt-in. The app does not run package-manager cleanup commands, install services, collect telemetry, or transmit source code. Selected cleanup moves eligible folders to the Windows Recycle Bin after confirmation.

Scanner behavior is intentionally conservative:

- Missing folders return `Exists = false` and `SizeInBytes = 0`.
- Locked or inaccessible files are skipped where possible.
- Cancellation tokens are supported through the scan contracts.
- High-risk targets are blocked unless policy explicitly allows them.
- Excluded paths are blocked from cleanup.
- Root drives and the user profile root are protected.

## Solution Structure

```text
src/
  DevClutterCleaner.Domain
  DevClutterCleaner.SharedKernel
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

Click `Scan caches` to scan registered cache locations and display individual and total disk usage. Use `Preview cleanup` to apply the current exclusions, export CSV/HTML/PDF reports after a scan, and use `Clean selected` only after reviewing the selected eligible rows.

## Roadmap

- Improve result sorting, filtering, and scan progress feedback.
- Improve PDF report layout beyond the current lightweight summary export.
- Add signed MSIX packaging using the checklist in `packaging/msix`.
- Expand plugin loading beyond built-in scanner plugins.
- Add UI, performance, regression, and compatibility test suites.
- Add Pro/Enterprise licensing, policy sync, and fleet dashboard features after the community MVP is stable.
