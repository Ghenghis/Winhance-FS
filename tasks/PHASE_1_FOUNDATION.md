# Phase 1: Foundation & Setup

> **[Back to Roadmap](PROJECT_ROADMAP.md)** | **Next Phase:** [Phase 2 - Rust Backend](PHASE_2_RUST_BACKEND.md)

---

## Phase Overview

| Attribute | Value |
|-----------|-------|
| **Status** | `[ ]` Not Started |
| **Priority** | Critical |
| **Estimated Tasks** | 15 |
| **Dependencies** | None (Starting Phase) |

---

## Objectives

1. Create the project directory structure following Winhance standards
2. Configure development environments for Rust, Python, and C#
3. Set up CI/CD pipelines for automated builds and testing
4. Establish code quality tools and linting configurations

---

## Task List

### 1.1 Repository Structure

#### Task 1.1.1: Create Rust Workspace

**Status:** `[ ]` Not Started

**Description:**
Create the `src/nexus-native` Rust workspace with proper Cargo configuration.

**Acceptance Criteria:**
- [ ] `src/nexus-native/Cargo.toml` exists with workspace configuration
- [ ] `src/nexus-native/src/lib.rs` exists with module structure
- [ ] `src/nexus-native/src/nexus.udl` exists with UniFFI definitions
- [ ] `cargo check` passes without errors

**Files to Create:**
```
src/nexus-native/
├── Cargo.toml
├── build.rs
├── src/
│   ├── lib.rs
│   ├── nexus.udl
│   ├── mft/
│   │   ├── mod.rs
│   │   └── parser.rs
│   ├── search/
│   │   ├── mod.rs
│   │   ├── simd.rs
│   │   └── bloom.rs
│   ├── index/
│   │   ├── mod.rs
│   │   └── tantivy.rs
│   ├── forensics/
│   │   ├── mod.rs
│   │   ├── ads.rs
│   │   ├── vss.rs
│   │   └── entropy.rs
│   └── ffi/
│       ├── mod.rs
│       └── callbacks.rs
├── benches/
│   └── benchmarks.rs
└── tests/
    └── integration_tests.rs
```

**Implementation Notes:**
```toml
# Cargo.toml template
[package]
name = "nexus-native"
version = "0.1.0"
edition = "2021"

[lib]
crate-type = ["cdylib", "staticlib"]
name = "nexus_native"

[dependencies]
ntfs = "0.4"
memchr = "2.7"
aho-corasick = "1.1"
fastbloom = "0.7"
tantivy = "0.22"
uniffi = { version = "0.28", features = ["cli"] }
tokio = { version = "1.40", features = ["full"] }
rayon = "1.10"
windows = { version = "0.58", features = ["Win32_Storage_FileSystem"] }

[build-dependencies]
uniffi = { version = "0.28", features = ["build"] }

[profile.release]
lto = "fat"
codegen-units = 1
panic = "abort"
strip = true
```

**Next Task:** [1.1.2](#task-112-create-python-package)

---

#### Task 1.1.2: Create Python Package

**Status:** `[ ]` Not Started

**Description:**
Create the `src/nexus-agents` Python package with proper structure.

**Acceptance Criteria:**
- [ ] `src/nexus-agents/pyproject.toml` exists
- [ ] Package structure follows Python best practices
- [ ] `pip install -e .` succeeds
- [ ] `pytest` runs without import errors

**Files to Create:**
```
src/nexus-agents/
├── pyproject.toml
├── README.md
├── nexus_agents/
│   ├── __init__.py
│   ├── core/
│   │   ├── __init__.py
│   │   ├── base_agent.py
│   │   ├── runtime.py
│   │   └── orchestrator.py
│   ├── agents/
│   │   ├── __init__.py
│   │   ├── file_discovery.py
│   │   ├── classification.py
│   │   ├── organization.py
│   │   └── cleanup.py
│   ├── mcp/
│   │   ├── __init__.py
│   │   └── server.py
│   └── utils/
│       ├── __init__.py
│       ├── logging.py
│       └── config.py
└── tests/
    ├── __init__.py
    ├── conftest.py
    └── test_agents.py
```

**Implementation Notes:**
```toml
# pyproject.toml template
[build-system]
requires = ["hatchling"]
build-backend = "hatchling.build"

[project]
name = "nexus-agents"
version = "0.1.0"
description = "AI Agents for Winhance-FS Storage Intelligence"
requires-python = ">=3.11"
dependencies = [
    "pydantic>=2.5",
    "openai>=1.12",
    "anthropic>=0.18",
    "playwright>=1.41",
    "fastmcp>=0.1",
    "aiohttp>=3.9",
]

[project.optional-dependencies]
dev = ["pytest>=8.0", "pytest-asyncio>=0.23", "black>=24.1", "ruff>=0.2"]
```

**Dependencies:** [1.1.1](#task-111-create-rust-workspace)

**Next Task:** [1.1.3](#task-113-create-c-project-structure)

---

#### Task 1.1.3: Create C# Project Structure

**Status:** `[ ]` Not Started

**Description:**
Add Storage Intelligence projects to the existing Winhance solution.

**Acceptance Criteria:**
- [ ] `src/Winhance.Core/Features/Storage/` namespace created
- [ ] `src/Winhance.Infrastructure/Features/Storage/` namespace created
- [ ] `src/Winhance.WPF/Features/Storage/` namespace created
- [ ] Solution builds without errors

**Files to Create:**
```
src/Winhance.Core/Features/Storage/
├── Models/
│   ├── DriveInfo.cs
│   ├── FileEntry.cs
│   ├── ScanResult.cs
│   └── SpaceRecommendation.cs
├── Interfaces/
│   ├── IStorageScanner.cs
│   ├── IDeepScanner.cs
│   └── IAIModelManager.cs
└── Enums/
    ├── FileCategory.cs
    └── ScanDepth.cs

src/Winhance.Infrastructure/Features/Storage/
├── Services/
│   ├── StorageIntelligenceService.cs
│   ├── DeepScanService.cs
│   ├── AIModelManagerService.cs
│   └── NexusNativeWrapper.cs
└── Helpers/
    └── StorageHelpers.cs

src/Winhance.WPF/Features/Storage/
├── Views/
│   ├── StorageIntelligenceView.xaml
│   ├── StorageIntelligenceView.xaml.cs
│   ├── DeepScanView.xaml
│   └── AIModelManagerView.xaml
├── ViewModels/
│   ├── StorageIntelligenceViewModel.cs
│   ├── DeepScanViewModel.cs
│   └── AIModelManagerViewModel.cs
└── Controls/
    ├── DriveUsageChart.xaml
    └── FileTreeView.xaml
```

**Implementation Notes:**
- Follow Winhance's existing namespace conventions
- Use `OperationResult<T>` for all service methods
- Apply `[ObservableProperty]` and `[RelayCommand]` from CommunityToolkit.Mvvm
- Register services in `src/Winhance.WPF/App.xaml.cs` DI container

**Dependencies:** [1.1.1](#task-111-create-rust-workspace)

**Next Task:** [1.2.1](#task-121-configure-rust-toolchain)

---

### 1.2 Development Environment

#### Task 1.2.1: Configure Rust Toolchain

**Status:** `[ ]` Not Started

**Description:**
Set up Rust development environment with required targets and tools.

**Acceptance Criteria:**
- [ ] `rust-toolchain.toml` specifies stable channel
- [ ] Windows MSVC target configured
- [ ] `cargo fmt` and `cargo clippy` work
- [ ] VS Code extensions documented

**Files to Create:**
```
src/nexus-native/rust-toolchain.toml
src/nexus-native/.cargo/config.toml
```

**Implementation Notes:**
```toml
# rust-toolchain.toml
[toolchain]
channel = "stable"
components = ["rustfmt", "clippy"]
targets = ["x86_64-pc-windows-msvc"]
```

```toml
# .cargo/config.toml
[build]
rustflags = ["-C", "target-cpu=native"]

[target.x86_64-pc-windows-msvc]
linker = "lld-link"
```

**Next Task:** [1.2.2](#task-122-configure-python-environment)

---

#### Task 1.2.2: Configure Python Environment

**Status:** `[ ]` Not Started

**Description:**
Set up Python development environment with virtual environment and tools.

**Acceptance Criteria:**
- [ ] `requirements.txt` and `requirements-dev.txt` exist
- [ ] `.python-version` specifies Python 3.11+
- [ ] `ruff.toml` configures linting
- [ ] Pre-commit hooks configured

**Files to Create:**
```
src/nexus-agents/requirements.txt
src/nexus-agents/requirements-dev.txt
src/nexus-agents/.python-version
src/nexus-agents/ruff.toml
```

**Implementation Notes:**
```toml
# ruff.toml
line-length = 88
target-version = "py311"

[lint]
select = ["E", "F", "I", "N", "W", "UP", "B", "C4", "SIM"]
ignore = ["E501"]

[format]
quote-style = "double"
indent-style = "space"
```

**Dependencies:** [1.2.1](#task-121-configure-rust-toolchain)

**Next Task:** [1.2.3](#task-123-configure-c-environment)

---

#### Task 1.2.3: Configure C# Environment

**Status:** `[ ]` Not Started

**Description:**
Ensure C# development environment matches Winhance standards.

**Acceptance Criteria:**
- [ ] `.editorconfig` has all required rules
- [ ] `Directory.Build.props` configured
- [ ] Code analyzers enabled
- [ ] `dotnet build` succeeds

**Files to Verify/Update:**
```
.editorconfig (already created)
Directory.Build.props
src/Winhance.Core/Winhance.Core.csproj
src/Winhance.Infrastructure/Winhance.Infrastructure.csproj
src/Winhance.WPF/Winhance.WPF.csproj
```

**Implementation Notes:**
- Ensure nullable reference types enabled: `<Nullable>enable</Nullable>`
- Enable implicit usings: `<ImplicitUsings>enable</ImplicitUsings>`
- Set language version: `<LangVersion>latest</LangVersion>`

**Dependencies:** [1.2.2](#task-122-configure-python-environment)

**Next Task:** [1.3.1](#task-131-create-github-actions-build-workflow)

---

### 1.3 CI/CD Pipelines

#### Task 1.3.1: Create GitHub Actions Build Workflow

**Status:** `[ ]` Not Started

**Description:**
Create GitHub Actions workflow for building all components.

**Acceptance Criteria:**
- [ ] `.github/workflows/build.yml` exists
- [ ] Workflow builds Rust, Python, and C# components
- [ ] Workflow runs on push and PR
- [ ] Artifacts are uploaded

**Files to Create:**
```yaml
# .github/workflows/build.yml
name: Build

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  build-rust:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: dtolnay/rust-toolchain@stable
      - name: Build nexus-native
        run: cargo build --release --manifest-path src/nexus-native/Cargo.toml
      - name: Run tests
        run: cargo test --manifest-path src/nexus-native/Cargo.toml
      - name: Upload artifacts
        uses: actions/upload-artifact@v4
        with:
          name: nexus-native
          path: src/nexus-native/target/release/*.dll

  build-python:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-python@v5
        with:
          python-version: '3.11'
      - name: Install dependencies
        run: |
          pip install -e src/nexus-agents[dev]
      - name: Run tests
        run: pytest src/nexus-agents/tests

  build-dotnet:
    runs-on: windows-latest
    needs: [build-rust]
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - name: Download Rust artifacts
        uses: actions/download-artifact@v4
        with:
          name: nexus-native
          path: src/nexus-native/target/release/
      - name: Restore
        run: dotnet restore Winhance.sln
      - name: Build
        run: dotnet build Winhance.sln -c Release --no-restore
      - name: Test
        run: dotnet test Winhance.sln -c Release --no-build
```

**Dependencies:** [1.2.3](#task-123-configure-c-environment)

**Next Task:** [1.3.2](#task-132-create-release-workflow)

---

#### Task 1.3.2: Create Release Workflow

**Status:** `[ ]` Not Started

**Description:**
Create GitHub Actions workflow for creating releases.

**Acceptance Criteria:**
- [ ] `.github/workflows/release.yml` exists
- [ ] Workflow triggers on version tags
- [ ] Creates GitHub release with installer
- [ ] Updates release notes

**Files to Create:**
```
.github/workflows/release.yml
```

**Dependencies:** [1.3.1](#task-131-create-github-actions-build-workflow)

**Next Task:** [1.4.1](#task-141-create-documentation-index)

---

### 1.4 Documentation Framework

#### Task 1.4.1: Create Documentation Index

**Status:** `[x]` Complete

**Description:**
Create the documentation structure in the `docs/` folder.

**Files Created:**
- [x] `docs/README.md`
- [x] `docs/ARCHITECTURE.md`
- [x] `docs/THEMING.md`
- [x] `docs/STORAGE.md`
- [x] `docs/PERFORMANCE.md`
- [x] `docs/AGENTS.md`
- [x] `docs/MCP_INTEGRATION.md`
- [x] `docs/RUST_BACKEND.md`
- [x] `docs/CONTRIBUTING.md`

**Next Task:** [1.4.2](#task-142-create-task-tracking-system)

---

#### Task 1.4.2: Create Task Tracking System

**Status:** `[x]` Complete

**Description:**
Create the interactive task tracking markdown files.

**Files Created:**
- [x] `tasks/PROJECT_ROADMAP.md`
- [x] `tasks/PHASE_1_FOUNDATION.md`
- [ ] `tasks/PHASE_2_RUST_BACKEND.md`
- [ ] `tasks/PHASE_3_CSHARP_INTEGRATION.md`
- [ ] `tasks/PHASE_4_UI_THEMING.md`
- [ ] `tasks/PHASE_5_AI_AGENTS.md`
- [ ] `tasks/PHASE_6_MCP_SERVER.md`
- [ ] `tasks/PHASE_7_TESTING.md`
- [ ] `tasks/PHASE_8_RELEASE.md`

---

## Phase Completion Checklist

- [ ] All 1.1.x tasks complete (Repository Structure)
- [ ] All 1.2.x tasks complete (Development Environment)
- [ ] All 1.3.x tasks complete (CI/CD Pipelines)
- [ ] All 1.4.x tasks complete (Documentation Framework)
- [ ] `dotnet build Winhance.sln` succeeds
- [ ] `cargo build --manifest-path src/nexus-native/Cargo.toml` succeeds
- [ ] `pip install -e src/nexus-agents` succeeds

---

## Notes for Windsurf/Claude Code

1. **Order matters** - Complete tasks in the order listed
2. **Check dependencies** - Ensure linked tasks are complete before starting
3. **Mark progress** - Update `[ ]` to `[x]` as tasks complete
4. **Reference docs** - Use the [docs/](../docs/) folder for implementation guidance
5. **Follow standards** - Match Winhance coding style exactly

---

**[Back to Roadmap](PROJECT_ROADMAP.md)** | **Next Phase:** [Phase 2 - Rust Backend](PHASE_2_RUST_BACKEND.md)
