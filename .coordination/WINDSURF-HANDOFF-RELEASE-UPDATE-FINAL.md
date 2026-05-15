# Windsurf Handoff: Finish Winhance-FS Release/Update Path

Date: 2026-05-15
Repo: `C:\Winhance-FS-Repo`
Branch: `main`
Origin: `https://github.com/Ghenghis/Winhance-FS.git`
Upstream: `https://github.com/memstechtips/Winhance.git`
Current HEAD during handoff: `31c9c8b Fix: Complete multi-layer audit and cleanup`

## Goal

Finish and verify the release/update flow so installed Winhance-FS builds update from `Ghenghis/Winhance-FS`, not the original creator repository.

Required user-facing behavior:

1. App starts or user chooses "Check for updates".
2. App calls `https://api.github.com/repos/Ghenghis/Winhance-FS/releases/latest`.
3. If a newer release exists and has an asset, app shows the update dialog.
4. App prefers `Winhance-Setup-*.exe` if present, otherwise chooses an architecture-matching Winhance ZIP.
5. App downloads and launches the selected asset.
6. If no release exists or no asset exists, app logs that state and does not show a broken update dialog.

## What Is Already Done

Updater source is corrected:

- `src/Winhance.Infrastructure/Features/Common/Services/VersionService.cs`
  - Checks `Ghenghis/Winhance-FS`.
  - Uses `User-Agent: Winhance-FS-Update-Checker`.
  - Handles 404/no releases as "no update".
  - Selects the best release asset.
  - Prefers setup/installer `.exe` assets over ZIP assets.
  - Avoids showing updates for same-version releases.
  - Supports test injection of current version.

Version comparison is corrected:

- `src/Winhance.Core/Features/Common/Models/VersionInfo.cs`
  - Parses `v25.12.12`, `v1.0.0`, `v1.0.0-alpha`.
  - Prevents same-version releases from being treated as updates just because GitHub has a publish date.
  - Allows migration from date-style builds such as `v25.12.12` to a newer published semantic release.

Release packaging is aligned:

- `scripts/release.ps1`
  - Accepts `-Version v1.0.0` or `-Version 1.0.0`.
  - Stamps `Version`, `AssemblyVersion`, `FileVersion`, and `InformationalVersion`.
  - Produces `Winhance-FS-vX.Y.Z-win-x64-Portable.zip`.
  - Produces matching `.sha256`.

GitHub Actions release job is aligned:

- `.github/workflows/build-release.yml`
  - Can publish on pushed `v*` tags.
  - Can publish on manual `workflow_dispatch`.
  - Uploads ZIP, EXE, MSIX, APPX, NUPKG, and SHA256 files to the GitHub Release.
  - Release notes now say `Winhance-FS`.

Links/scripts moved off old creator repo:

- Bug report links now use `https://github.com/Ghenghis/Winhance-FS/issues`.
- Generated script source comments use `https://github.com/Ghenghis/Winhance-FS`.
- Legacy release typo corrected from `Ghengis` to `Ghenghis`.

File Manager runtime blockers are also fixed:

- `src/Winhance.WPF/Features/Common/Extensions/DI/ViewModelExtensions.cs`
  - Registers File Manager child view models required by runtime navigation.
- `src/Winhance.WPF/Features/Common/Extensions/DI/ViewExtensions.cs`
  - Registers File Manager views for the frame navigation/view pool.

## Verification Already Run

Commands run successfully:

```powershell
dotnet test .\tests\Winhance.Tests\Winhance.Tests.csproj --configuration Debug --no-restore --nologo /clp:Summary /v:minimal
```

Result: 130 passed.

```powershell
dotnet build .\Winhance.sln --configuration Release --no-restore --nologo /clp:Summary /v:minimal
```

Result: Release build passed, 0 warnings, 0 errors.

```powershell
.\scripts\release.ps1 -Version v0.0.2-audit -Runtime win-x64 -SkipTests
```

Produced:

- `C:\Winhance-FS-Repo\artifacts\release\v0.0.2-audit\Winhance-FS-v0.0.2-audit-win-x64-Portable.zip`
- `C:\Winhance-FS-Repo\artifacts\release\v0.0.2-audit\Winhance-FS-v0.0.2-audit-win-x64-Portable.zip.sha256`

Published binary version proof from local package build:

- `Winhance.exe`
- ProductVersion: `0.0.2-audit+31c9c8b0e602ef378538a4d5f0f08f8cfdc12941`
- FileVersion: `0.0.2.0`

Runtime smoke already passed before the latest handoff update:

- App startup completed.
- Update check logged `Checking for updates from Ghenghis/Winhance-FS...`.
- Current GitHub endpoint returned no published release, so app logged no update.
- File Manager navigation completed successfully.
- Fresh log failure count: 0.

Important current GitHub state:

```powershell
Invoke-RestMethod -Uri 'https://api.github.com/repos/Ghenghis/Winhance-FS/releases/latest' -Headers @{ 'User-Agent' = 'Winhance-FS-Audit' }
```

Current result: 404 Not Found because no GitHub Release is published yet.

## Files To Stage For This Work

The worktree is very dirty and includes many pre-existing/generated files. Do not blanket-revert. Stage only the intended files for the release/update fix plus the audit fixes the user wants preserved.

Core release/update files:

- `.github/workflows/build-release.yml`
- `.github/ISSUE_TEMPLATE/config.yml`
- `scripts/release.ps1`
- `scripts/release-legacy.ps1`
- `docs/UPDATE-RELEASE-GUIDE.md`
- `.coordination/WINDSURF-HANDOFF-RELEASE-UPDATE-FINAL.md`
- `src/Winhance.Core/Features/Common/Models/VersionInfo.cs`
- `src/Winhance.Infrastructure/Features/Common/Services/VersionService.cs`
- `src/Winhance.Infrastructure/Features/AdvancedTools/Services/AutounattendScriptBuilder.cs`
- `src/Winhance.Core/Features/SoftwareApps/Utilities/BloatRemovalScriptGenerator.cs`
- `src/Winhance.Core/Features/SoftwareApps/Models/EdgeRemovalScript.cs`
- `src/Winhance.Core/Features/SoftwareApps/Models/OneDriveRemovalScript.cs`
- `src/Winhance.WPF/Features/Common/ViewModels/MainViewModel.cs`
- `src/Winhance.WPF/Features/Common/ViewModels/MoreMenuViewModel.cs`
- `tests/Winhance.Tests/Common/VersionInfoTests.cs`
- `tests/Winhance.Tests/Common/VersionServiceTests.cs`

File Manager/buildability fixes from this audit:

- `Winhance.sln`
- `src/Winhance.WPF/Features/Common/Extensions/DI/ViewExtensions.cs`
- `src/Winhance.WPF/Features/Common/Extensions/DI/ViewModelExtensions.cs`
- `src/Winhance.WPF/Features/Common/Resources/Converters/Converters.xaml`
- `src/Winhance.WPF/Features/Common/Converters/FileManagerUiConverters.cs`
- `src/Winhance.WPF/Features/FileManager/ViewModels/FileListViewModel.cs`
- `src/Winhance.WPF/Features/FileManager/ViewModels/FileItemViewModel.cs`
- `src/Winhance.WPF/Features/FileManager/ViewModels/FileManagerViewModel.cs`
- `src/Winhance.WPF/Features/FileManager/ViewModels/DualPaneBrowserViewModel.cs`
- `src/Winhance.WPF/Features/FileManager/Views/FileManagerView.xaml`
- `tests/Winhance.Tests/FileManager/FileListViewModelTests.cs`
- `tests/Winhance.Tests/FileManager/FileManagerViewModelTests.cs`
- `tests/Winhance.Tests/FileManager/DualPaneBrowserViewModelTests.cs`

Review these paths before staging because the repo has many unrelated modified/untracked files.

## Windsurf Must Finish

### 1. Commit and push the intended changes

Suggested local verification first:

```powershell
dotnet test .\Winhance.sln --configuration Debug --no-restore --nologo /clp:Summary /v:minimal
dotnet build .\Winhance.sln --configuration Release --no-restore --nologo /clp:Summary /v:minimal
.\scripts\release.ps1 -Version v1.0.0 -Runtime win-x64 -SkipTests
```

Then commit only intended files:

```powershell
git add <selected files from this handoff>
git commit -m "Fix Winhance-FS release update channel"
git push origin main
```

### 2. Publish a GitHub Release in `Ghenghis/Winhance-FS`

Preferred: use GitHub Actions after pushing.

Option A: pushed tag:

```powershell
git tag -a v1.0.0 -m "Winhance-FS v1.0.0"
git push origin v1.0.0
```

Option B: GitHub Actions manual dispatch:

- Open `https://github.com/Ghenghis/Winhance-FS/actions`
- Run `Build and Release`
- Set `version` to `v1.0.0`

The release must contain at least one asset. Best asset for update testing:

- `Winhance-Setup-v1.0.0.exe`

Acceptable fallback asset:

- `Winhance-x64-v1.0.0.zip`
- `Winhance-FS-v1.0.0-win-x64-Portable.zip`

### 3. Verify GitHub release endpoint

After publishing:

```powershell
$release = Invoke-RestMethod -Uri 'https://api.github.com/repos/Ghenghis/Winhance-FS/releases/latest' -Headers @{ 'User-Agent' = 'Winhance-FS-Audit' }
$release.tag_name
$release.assets | Select-Object name,browser_download_url
```

Expected:

- HTTP 200.
- `tag_name` is the release tag.
- Assets list includes the setup EXE or Winhance ZIP.

### 4. Verify app update behavior

Use an installed/local app build with a lower current version than the GitHub Release.

Expected logs under `C:\ProgramData\Winhance\Logs`:

- `Checking for updates from Ghenghis/Winhance-FS...`
- `Current version: ..., Latest version: v1.0.0, Update available: True`

Expected UI:

- Update dialog appears on startup or manual check.
- Clicking install downloads the selected GitHub asset.
- If `Winhance-Setup-v1.0.0.exe` exists, that should be selected before ZIPs.

If testing with the exact same version as the app binary, update dialog should not appear.

### 5. Validate File Manager route still opens

Run WPF smoke:

- Launch `src\Winhance.WPF\bin\Debug\net9.0-windows\Winhance.exe`.
- Click `File Manager`.
- Check latest app log for no `PreloadAndNavigateToAsync failed`, no `No service for type`, no `Cannot find resource`.

Known previous passing log line:

- `Navigation completed successfully to: FileManager`

## Remaining Non-Release Work

These are not blockers for the update-channel fix, but still remain before calling the full product complete:

- `cargo` was not installed locally, so Rust crates were not validated here.
- There are many generated `Feature###View.xaml` placeholders under File Manager views; they compile but should be reviewed/removed or wired intentionally.
- Some File Manager commands still contain no-op comments, especially advanced selection/context menu/network location polish.
- Run a full manual UX pass after the release publish, especially update dialog install flow and File Manager file operations.

## Completion Criteria

Windsurf can mark this complete when all are true:

- Changes are committed and pushed to `Ghenghis/Winhance-FS`.
- `https://api.github.com/repos/Ghenghis/Winhance-FS/releases/latest` returns 200.
- Latest release has at least one Winhance asset.
- App logs update checks against `Ghenghis/Winhance-FS`, not `memstechtips/Winhance`.
- Older app build shows the update dialog for the new release.
- Same-version app build does not show an update dialog.
- Install/update button downloads the setup EXE when present.
- `dotnet test .\Winhance.sln --configuration Debug --no-restore --nologo /clp:Summary /v:minimal` passes.
- `dotnet build .\Winhance.sln --configuration Release --no-restore --nologo /clp:Summary /v:minimal` passes.
