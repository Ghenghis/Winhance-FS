# QUICK START - Parallel Development

## For Claude Code (Backend Agent)

### Your Territory
```
✅ src/nexus_core/           (Rust - ALL)
✅ src/nexus_cli/            (Python CLI - ALL)
✅ src/nexus_mcp/            (MCP Server - ALL)
✅ src/nexus_ai/             (Python AI - ALL)
✅ src/Winhance.Core/.../Interfaces/
✅ src/Winhance.Infrastructure/Features/FileManager/
✅ tests/
✅ scripts/
```

### First Tasks (Phase 1, Week 1)
```
CC-P1-001: nexus_core/src/indexer/mft_reader.rs
CC-P1-002: nexus_core/src/indexer/usn_journal.rs
CC-P1-003: nexus_core/src/indexer/content_hasher.rs
CC-P1-005: Winhance.Core/.../Interfaces/ISearchService.cs
CC-P1-006: Winhance.Infrastructure/.../Services/SearchService.cs
```

### Commands
```bash
# Update your status
edit .coordination/status/claude-code-status.md

# Check Windsurf status
cat .coordination/status/windsurf-status.md

# Build Rust
cd src/nexus_core && cargo build

# Build C#
dotnet build src/Winhance.Infrastructure
```

---

## For Windsurf (Frontend Agent)

### Your Territory
```
✅ src/Winhance.WPF/Features/FileManager/Views/
✅ src/Winhance.WPF/Features/FileManager/ViewModels/
✅ src/Winhance.WPF/Features/FileManager/Controls/
✅ src/Winhance.WPF/Features/FileManager/Converters/
✅ src/Winhance.WPF/Features/FileManager/Resources/
✅ src/Winhance.WPF/Features/Common/Controls/
✅ docs/
```

### First Tasks (Phase 1, Week 1)
```
WS-P1-001: Views/TabContainerView.xaml
WS-P1-002: ViewModels/TabContainerViewModel.cs
WS-P1-003: Controls/SearchBox.xaml
WS-P1-004: Views/SearchResultsView.xaml
WS-P1-005: ViewModels/SearchResultsViewModel.cs
WS-P1-006: Views/FavoritesPanel.xaml
```

### Commands
```bash
# Update your status
edit .coordination/status/windsurf-status.md

# Check Claude status
cat .coordination/status/claude-code-status.md

# Build WPF
dotnet build src/Winhance.WPF
```

---

## Shared Rules

### Before Editing Shared Files
1. Check `.coordination/locks/` for locks
2. Create lock: `echo "AgentName" > .coordination/locks/filename.lock`
3. Edit file
4. Remove lock when done

### When You Complete a Task
1. Update `.coordination/completed/[agent]-completed.md`
2. Update `.coordination/status/[agent]-status.md`
3. Notify other agent if they depend on your work

### When You're Blocked
1. Update status with blocker
2. Add notification to `.coordination/notifications/to-[other-agent].md`
3. Work on different task

---

## Interface Contracts

See `.coordination/interfaces/contracts.md` for:
- ISearchService
- ITabService
- IFavoritesService
- IOperationQueueService

Claude creates interfaces → Windsurf consumes them
