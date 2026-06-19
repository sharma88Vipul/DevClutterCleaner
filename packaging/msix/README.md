# MSIX Packaging

This folder is the MSIX packaging handoff point for the Windows desktop edition.

Current status:

- CI publishes the WPF application as a Windows `win-x64` artifact.
- A full signed MSIX requires a publisher identity, certificate, app icons, and a Windows Application Packaging Project or equivalent `makeappx` pipeline.
- Keep cleanup behavior behind explicit confirmation and Recycle Bin execution before publishing signed installers.

Release checklist:

1. Create or import the signing certificate.
2. Add production app icons and publisher metadata.
3. Generate the MSIX package from the published `DevClutterCleaner.UI` output.
4. Sign the MSIX.
5. Smoke test install, scan, report export, exclusion persistence, and Recycle Bin cleanup.
