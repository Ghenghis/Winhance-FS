# Winhance-FS Update Release Guide

Winhance-FS checks GitHub Releases from:

`https://api.github.com/repos/Ghenghis/Winhance-FS/releases/latest`

## Required Release Shape

Publish releases in `Ghenghis/Winhance-FS` using a tag such as:

- `v1.0.0`
- `v1.0.1`
- `v0.1.0-alpha`

Attach at least one downloadable asset. Preferred assets:

- `Winhance-Setup-v1.0.0.exe`
- `Winhance-x64-v1.0.0.zip`
- `Winhance-FS-v1.0.0-win-x64-Portable.zip`

When both an installer and ZIP are present, the app prefers the setup installer.

## Expected App Behavior

On startup and manual update check, the app:

1. Calls the latest release endpoint for `Ghenghis/Winhance-FS`.
2. Parses the release tag and published date.
3. Selects the best matching release asset for the current Windows architecture.
4. Shows the update dialog only when the release is newer and has a downloadable asset.
5. Downloads and launches the selected asset.

If no release exists, or a release has no downloadable assets, the app logs the condition and does not show a broken update dialog.

## Local Release Package

Create a local portable package with:

```powershell
.\scripts\release.ps1 -Version v1.0.0 -Runtime win-x64
```

This produces a version-stamped ZIP plus a `.sha256` checksum under:

`artifacts\release\v1.0.0\`

## GitHub Release Workflow

The `Build and Release` workflow publishes a GitHub Release when either:

- A `v*` tag is pushed, or
- The workflow is manually dispatched with a version value.

The release job uploads ZIPs, installer EXEs, Windows package assets, package-manager artifacts, and SHA256 checksums.
