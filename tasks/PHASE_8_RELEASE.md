# Phase 8: Release & Deployment

> **[Back to Roadmap](PROJECT_ROADMAP.md)** | **Previous:** [Phase 7](PHASE_7_TESTING.md)

---

## Phase Overview

| Attribute | Value |
|-----------|-------|
| **Status** | `[ ]` Not Started |
| **Priority** | Critical |
| **Estimated Tasks** | 14 |
| **Dependencies** | All Previous Phases Complete |

---

## Objectives

1. Create Windows installer using Inno Setup
2. Finalize all documentation
3. Write comprehensive release notes
4. Configure GitHub release automation
5. Publish to GitHub Releases

---

## Task List

### 8.1 Installer Creation

#### Task 8.1.1: Create Inno Setup Script

**Status:** `[ ]` Not Started

**Description:**
Create Inno Setup script for Windows installer.

**Acceptance Criteria:**
- [ ] Single executable installer
- [ ] Includes all components (C#, Rust DLL, Python)
- [ ] Desktop and Start Menu shortcuts
- [ ] Uninstaller included
- [ ] Version info embedded

**Files to Create:**
```
installer/winhance-fs.iss
installer/assets/
installer/assets/icon.ico
installer/assets/wizard.bmp
installer/assets/license.rtf
```

**Implementation:**
```iss
; winhance-fs.iss
#define MyAppName "Winhance-FS"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Winhance"
#define MyAppURL "https://github.com/Ghenghis/Winhance-FS"
#define MyAppExeName "Winhance.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=assets\license.rtf
OutputDir=..\dist
OutputBaseFilename=Winhance-FS-{#MyAppVersion}-Setup
SetupIconFile=assets\icon.ico
WizardImageFile=assets\wizard.bmp
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64
MinVersion=10.0.17763

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
; Main application
Source: "..\src\Winhance.WPF\bin\Release\net8.0-windows\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Rust native library
Source: "..\src\nexus-native\target\release\nexus_native.dll"; DestDir: "{app}"; Flags: ignoreversion

; Python components
Source: "..\src\nexus-agents\dist\nexus_agents-*-py3-none-any.whl"; DestDir: "{app}\python"; Flags: ignoreversion

; Documentation
Source: "..\docs\*"; DestDir: "{app}\docs"; Flags: ignoreversion recursesubdirs createallsubdirs

; MCP configurations
Source: "..\configs\*"; DestDir: "{app}\configs"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
begin
  // Check for .NET 8 runtime
  Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Post-installation tasks
    // Register MCP server, etc.
  end;
end;

[UninstallDelete]
Type: filesandordirs; Name: "{app}\logs"
Type: filesandordirs; Name: "{app}\cache"
```

**Next Task:** [8.1.2](#task-812-build-release-artifacts)

---

#### Task 8.1.2: Build Release Artifacts

**Status:** `[ ]` Not Started

**Description:**
Create automated build script for release artifacts.

**Acceptance Criteria:**
- [ ] Single command builds all components
- [ ] Version numbers synchronized
- [ ] Debug symbols stripped
- [ ] Artifacts collected in dist/

**Files to Create:**
```
scripts/build-release.ps1
scripts/build-release.sh
```

**Implementation:**
```powershell
# build-release.ps1
param(
    [string]$Version = "1.0.0",
    [switch]$SkipTests
)

$ErrorActionPreference = "Stop"
$RootDir = Split-Path -Parent $PSScriptRoot

Write-Host "Building Winhance-FS v$Version" -ForegroundColor Cyan

# Create dist directory
$DistDir = Join-Path $RootDir "dist"
if (Test-Path $DistDir) {
    Remove-Item $DistDir -Recurse -Force
}
New-Item -ItemType Directory -Path $DistDir | Out-Null

# Build Rust components
Write-Host "`nBuilding Rust backend..." -ForegroundColor Yellow
Push-Location (Join-Path $RootDir "src\nexus-native")
cargo build --release
if (-not $SkipTests) {
    cargo test --release
}
Pop-Location

# Build .NET application
Write-Host "`nBuilding .NET application..." -ForegroundColor Yellow
Push-Location $RootDir
dotnet publish src\Winhance.WPF\Winhance.WPF.csproj `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -p:PublishSingleFile=false `
    -p:Version=$Version `
    -o "$DistDir\app"
Pop-Location

# Copy Rust DLL
Write-Host "`nCopying Rust native library..." -ForegroundColor Yellow
Copy-Item `
    (Join-Path $RootDir "src\nexus-native\target\release\nexus_native.dll") `
    (Join-Path $DistDir "app")

# Build Python wheel
Write-Host "`nBuilding Python package..." -ForegroundColor Yellow
Push-Location (Join-Path $RootDir "src\nexus-agents")
pip install build
python -m build --wheel --outdir "$DistDir\python"
Pop-Location

# Run tests
if (-not $SkipTests) {
    Write-Host "`nRunning tests..." -ForegroundColor Yellow
    Push-Location $RootDir
    dotnet test Winhance.sln -c Release --no-build
    Pop-Location

    Push-Location (Join-Path $RootDir "src\nexus-agents")
    pytest tests/ -v
    Pop-Location
}

# Build installer
Write-Host "`nBuilding installer..." -ForegroundColor Yellow
Push-Location (Join-Path $RootDir "installer")
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" `
    /DMyAppVersion=$Version `
    winhance-fs.iss
Pop-Location

Write-Host "`nBuild complete!" -ForegroundColor Green
Write-Host "Artifacts in: $DistDir"
```

**Dependencies:** [8.1.1](#task-811-create-inno-setup-script)

**Next Task:** [8.2.1](#task-821-finalize-readme)

---

### 8.2 Documentation Finalization

#### Task 8.2.1: Finalize README

**Status:** `[ ]` Not Started

**Description:**
Update main README with final content.

**Acceptance Criteria:**
- [ ] Clear installation instructions
- [ ] Feature highlights with screenshots
- [ ] Quick start guide
- [ ] Links to documentation

**Files to Update:**
```
README.md
```

**Template:**
```markdown
# Winhance-FS

<p align="center">
  <img src="assets/logo.png" alt="Winhance-FS Logo" width="200">
</p>

<p align="center">
  <strong>Storage Intelligence for Windows 11</strong>
</p>

<p align="center">
  <a href="#features">Features</a> •
  <a href="#installation">Installation</a> •
  <a href="#quick-start">Quick Start</a> •
  <a href="#documentation">Documentation</a> •
  <a href="#contributing">Contributing</a>
</p>

---

## Features

### Storage Intelligence
- **Deep Space Analysis** - Scan drives faster than Everything Search
- **Smart Recommendations** - AI-powered cleanup suggestions
- **AI Model Manager** - Manage your LLM models across drives

### Borg Theme Studio
- **5-Color Customization** - Click-to-change theming
- **8 Preset Palettes** - Borg Green, Red, Blue, Purple, Gold, Orange, Pink, Neon
- **Import/Export** - Share your themes

### MCP Integration
- **AI Agent Tools** - 8 powerful MCP tools
- **IDE Support** - Claude Code, Windsurf, Cursor, LM Studio
- **Automation** - Multi-agent workflows

## Installation

### Requirements
- Windows 11 (build 22000+)
- .NET 8.0 Runtime
- Python 3.11+ (for AI agents)

### Download
Download the latest installer from [Releases](https://github.com/Ghenghis/Winhance-FS/releases).

### Manual Installation
```bash
# Clone repository
git clone https://github.com/Ghenghis/Winhance-FS.git
cd Winhance-FS

# Build
dotnet build Winhance.sln -c Release

# Install Python agents
pip install -e src/nexus-agents
```

## Quick Start

1. **Launch Winhance-FS**
2. **Select a drive** from the dropdown
3. **Click Scan** to analyze storage
4. **Review recommendations** and apply as needed

### MCP Setup (Claude Code)

Add to your MCP configuration:

```json
{
  "mcpServers": {
    "winhance-fs": {
      "command": "python",
      "args": ["-m", "nexus_agents.mcp"]
    }
  }
}
```

## Documentation

| Document | Description |
|----------|-------------|
| [Architecture](docs/ARCHITECTURE.md) | System design |
| [Theming](docs/THEMING.md) | Borg Theme Studio guide |
| [MCP Integration](docs/MCP_INTEGRATION.md) | AI tool setup |
| [Contributing](CONTRIBUTING.md) | How to contribute |

## Contributing

We welcome contributions! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

This project is licensed under the MIT License - see [LICENSE](LICENSE) for details.

## Acknowledgments

- Built on [Winhance](https://github.com/Ghenghis/Winhance)
- Rust performance crates: ntfs, memchr, tantivy
- FastMCP for MCP server
```

**Dependencies:** [8.1.2](#task-812-build-release-artifacts)

**Next Task:** [8.2.2](#task-822-create-changelog)

---

#### Task 8.2.2: Create CHANGELOG

**Status:** `[ ]` Not Started

**Description:**
Create changelog following Keep a Changelog format.

**Files to Create:**
```
CHANGELOG.md
```

**Template:**
```markdown
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-01-XX

### Added
- Storage Intelligence module with drive scanning
- Deep Scan features (ADS, VSS, entropy analysis)
- AI Model Manager for LLM model management
- Borg Theme Studio with 8 preset palettes
- MCP Server with 8 tools for AI integration
- Python agent framework with 4 specialized agents
- Rust backend (nexus-native) for high performance
- CLI tool for command-line operations

### Performance
- MFT scanning < 1 second for 1M files
- Search latency < 5ms
- Memory usage < 30MB per 1M files

### Documentation
- Complete architecture documentation
- Theming guide
- MCP integration guide
- Contributing guidelines

[Unreleased]: https://github.com/Ghenghis/Winhance-FS/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/Ghenghis/Winhance-FS/releases/tag/v1.0.0
```

**Dependencies:** [8.2.1](#task-821-finalize-readme)

**Next Task:** [8.3.1](#task-831-write-release-notes)

---

### 8.3 Release Notes

#### Task 8.3.1: Write Release Notes

**Status:** `[ ]` Not Started

**Description:**
Write detailed release notes for v1.0.0.

**Files to Create:**
```
RELEASE_NOTES.md
```

**Template:**
```markdown
# Winhance-FS v1.0.0 Release Notes

**Release Date:** 2026-01-XX

We're excited to announce the first release of Winhance-FS, the Storage Intelligence extension for Winhance!

## Highlights

### Storage Intelligence
Analyze your drives with unprecedented speed and detail:
- **MFT Direct Access** - Scan 1 million files in under 1 second
- **Smart Recommendations** - AI-powered suggestions for freeing space
- **Category Breakdown** - See exactly what's using your storage

### Borg Theme Studio
Customize your experience with our unique theming system:
- **5 Color Slots** - Primary, Secondary, Accent, Text, Background
- **Click-to-Change** - Simply click any color to customize
- **8 Borg Palettes** - Green, Red, Blue, Purple, Gold, Orange, Pink, Neon

### AI Integration
Connect Winhance-FS to your favorite AI tools:
- **MCP Server** - 8 powerful tools for AI agents
- **Multi-IDE Support** - Claude Code, Windsurf, Cursor, LM Studio
- **Python Agents** - Automated file organization and cleanup

## Installation

### System Requirements
- Windows 11 (build 22000 or later)
- .NET 8.0 Runtime
- 4GB RAM minimum
- Python 3.11+ (optional, for AI agents)

### Download
1. Download `Winhance-FS-1.0.0-Setup.exe` from the assets below
2. Run the installer
3. Follow the setup wizard

## Known Issues
- [#XX] Description of known issue

## Feedback
Please report issues on [GitHub Issues](https://github.com/Ghenghis/Winhance-FS/issues).

## Contributors
Thanks to everyone who contributed to this release!

---

**Full Changelog:** https://github.com/Ghenghis/Winhance-FS/compare/...v1.0.0
```

**Dependencies:** [8.2.2](#task-822-create-changelog)

**Next Task:** [8.4.1](#task-841-configure-github-release)

---

### 8.4 GitHub Release

#### Task 8.4.1: Configure GitHub Release Workflow

**Status:** `[ ]` Not Started

**Description:**
Create GitHub Actions workflow for automated releases.

**Files to Create:**
```
.github/workflows/release.yml
```

**Implementation:**
```yaml
name: Release

on:
  push:
    tags:
      - 'v*'

permissions:
  contents: write

jobs:
  build:
    runs-on: windows-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Setup Rust
        uses: dtolnay/rust-toolchain@stable

      - name: Setup Python
        uses: actions/setup-python@v5
        with:
          python-version: '3.11'

      - name: Get version from tag
        id: version
        run: echo "VERSION=${GITHUB_REF#refs/tags/v}" >> $GITHUB_OUTPUT
        shell: bash

      - name: Build Rust
        run: cargo build --release --manifest-path src/nexus-native/Cargo.toml

      - name: Build .NET
        run: |
          dotnet publish src/Winhance.WPF/Winhance.WPF.csproj `
            -c Release `
            -r win-x64 `
            --self-contained false `
            -p:Version=${{ steps.version.outputs.VERSION }}

      - name: Build Python
        run: |
          pip install build
          python -m build --wheel src/nexus-agents

      - name: Build Installer
        run: |
          & "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" `
            /DMyAppVersion=${{ steps.version.outputs.VERSION }} `
            installer/winhance-fs.iss

      - name: Create Release
        uses: softprops/action-gh-release@v1
        with:
          files: |
            dist/Winhance-FS-${{ steps.version.outputs.VERSION }}-Setup.exe
            src/nexus-agents/dist/*.whl
          body_path: RELEASE_NOTES.md
          draft: false
          prerelease: false
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

**Dependencies:** [8.3.1](#task-831-write-release-notes)

**Next Task:** [8.4.2](#task-842-create-release)

---

#### Task 8.4.2: Create Release

**Status:** `[ ]` Not Started

**Description:**
Create and publish the first release.

**Acceptance Criteria:**
- [ ] Version tag created
- [ ] Release builds successfully
- [ ] Installer works on clean Windows 11
- [ ] Documentation accessible

**Steps:**
```bash
# Create version tag
git tag -a v1.0.0 -m "Release v1.0.0"

# Push tag to trigger release workflow
git push origin v1.0.0

# Verify release at:
# https://github.com/Ghenghis/Winhance-FS/releases/tag/v1.0.0
```

**Dependencies:** [8.4.1](#task-841-configure-github-release)

---

## Phase Completion Checklist

- [ ] All 8.1.x tasks complete (Installer Creation)
- [ ] All 8.2.x tasks complete (Documentation)
- [ ] All 8.3.x tasks complete (Release Notes)
- [ ] All 8.4.x tasks complete (GitHub Release)
- [ ] Installer tested on clean Windows 11
- [ ] All documentation reviewed
- [ ] Release published to GitHub

---

## Post-Release Checklist

- [ ] Announce release on social media
- [ ] Update project website
- [ ] Monitor issues for bug reports
- [ ] Respond to user feedback
- [ ] Plan next release milestones

---

## Congratulations!

You have completed all phases of the Winhance-FS project!

**[Back to Roadmap](PROJECT_ROADMAP.md)**
