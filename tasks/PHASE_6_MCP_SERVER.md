# Phase 6: MCP Server Integration

> **[Back to Roadmap](PROJECT_ROADMAP.md)** | **Previous:** [Phase 5](PHASE_5_AI_AGENTS.md) | **Next:** [Phase 7](PHASE_7_TESTING.md)

---

## Phase Overview

| Attribute | Value |
|-----------|-------|
| **Status** | `[ ]` Not Started |
| **Priority** | High |
| **Estimated Tasks** | 12 |
| **Dependencies** | [Phase 5](PHASE_5_AI_AGENTS.md) Complete |

---

## Objectives

1. Create MCP server using FastMCP
2. Expose all Storage Intelligence tools
3. Configure for multiple IDEs (Claude Code, Windsurf, Cursor)
4. Implement security controls
5. Add streaming and progress support

---

## MCP Architecture

```
+==============================================================================+
|                           MCP SERVER ARCHITECTURE                             |
+==============================================================================+
|                                                                               |
|  IDE/Client                        MCP Server                                 |
|  +------------------+             +---------------------------+               |
|  | Claude Code      |             |                           |               |
|  | Windsurf         |<--stdio---->|  FastMCP Server           |               |
|  | Cursor           |             |                           |               |
|  | LM Studio        |             |  Tools:                   |               |
|  +------------------+             |  - nexus_scan             |               |
|                                   |  - nexus_search           |               |
|                                   |  - nexus_move             |               |
|                                   |  - nexus_delete           |               |
|                                   |  - nexus_organize         |               |
|                                   |  - nexus_analyze          |               |
|                                   |                           |               |
|                                   +-------------+-------------+               |
|                                                 |                             |
|                                                 v                             |
|                                   +---------------------------+               |
|                                   |    nexus-native (Rust)    |               |
|                                   |    nexus-agents (Python)  |               |
|                                   +---------------------------+               |
|                                                                               |
+==============================================================================+
```

---

## Task List

### 6.1 MCP Server Core

#### Task 6.1.1: Create FastMCP Server

**Status:** `[ ]` Not Started

**Description:**
Create the main MCP server using FastMCP framework.

**Acceptance Criteria:**
- [ ] Server starts and accepts connections
- [ ] Handles stdio transport
- [ ] Proper error handling
- [ ] Logging configuration

**Files to Create:**
```
src/nexus-agents/nexus_agents/mcp/server.py
src/nexus-agents/nexus_agents/mcp/__main__.py
```

**Implementation:**
```python
# server.py
from fastmcp import FastMCP
from typing import Any, Dict, List, Optional
import logging

# Initialize FastMCP server
mcp = FastMCP("Winhance-FS Storage Intelligence")

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("nexus-mcp")


@mcp.tool()
async def nexus_scan(
    drive_letter: str | None = None,
    include_hidden: bool = True,
    max_depth: int = 0
) -> Dict[str, Any]:
    """
    Scan a drive for space analysis.

    Args:
        drive_letter: Drive to scan (e.g., "C"). If None, scans all drives.
        include_hidden: Whether to include hidden files.
        max_depth: Maximum directory depth (0 = unlimited).

    Returns:
        Scan results with file counts, sizes, and categories.
    """
    from ..core.storage import StorageScanner

    scanner = StorageScanner()
    if drive_letter:
        result = await scanner.scan_drive(
            drive_letter,
            include_hidden=include_hidden,
            max_depth=max_depth
        )
    else:
        result = await scanner.scan_all_drives(
            include_hidden=include_hidden,
            max_depth=max_depth
        )

    return {
        "success": True,
        "total_files": result.file_count,
        "total_size": result.total_size,
        "total_size_formatted": format_size(result.total_size),
        "categories": result.categories,
        "scan_duration_ms": result.duration_ms
    }


@mcp.tool()
async def nexus_search(
    query: str,
    path: str | None = None,
    case_sensitive: bool = False,
    use_regex: bool = False,
    max_results: int = 100
) -> Dict[str, Any]:
    """
    Search for files matching a query.

    Args:
        query: Search query (filename pattern or regex).
        path: Path to search in (default: all indexed drives).
        case_sensitive: Case-sensitive search.
        use_regex: Treat query as regex pattern.
        max_results: Maximum results to return.

    Returns:
        List of matching files with paths and metadata.
    """
    from ..core.search import SearchEngine

    engine = SearchEngine()
    results = await engine.search(
        query=query,
        path=path,
        case_sensitive=case_sensitive,
        use_regex=use_regex,
        limit=max_results
    )

    return {
        "success": True,
        "query": query,
        "count": len(results),
        "results": [
            {
                "path": r.path,
                "name": r.name,
                "size": r.size,
                "modified": r.modified.isoformat() if r.modified else None,
                "score": r.score
            }
            for r in results
        ]
    }


@mcp.tool()
async def nexus_move(
    source: str | List[str],
    destination: str,
    create_symlink: bool = False,
    overwrite: bool = False
) -> Dict[str, Any]:
    """
    Move files or directories to a new location.

    Args:
        source: Source path(s) to move.
        destination: Destination directory.
        create_symlink: Create symlink at original location.
        overwrite: Overwrite existing files.

    Returns:
        Operation result with moved files list.
    """
    from ..core.operations import FileOperations

    ops = FileOperations()
    sources = [source] if isinstance(source, str) else source

    results = await ops.move_files(
        sources=sources,
        destination=destination,
        create_symlink=create_symlink,
        overwrite=overwrite
    )

    return {
        "success": results.success,
        "moved_count": results.moved_count,
        "failed_count": results.failed_count,
        "symlinks_created": results.symlinks_created,
        "errors": results.errors
    }


@mcp.tool()
async def nexus_delete(
    paths: str | List[str],
    permanent: bool = False,
    dry_run: bool = True
) -> Dict[str, Any]:
    """
    Delete files or directories.

    Args:
        paths: Path(s) to delete.
        permanent: Permanently delete (bypass recycle bin).
        dry_run: Only simulate deletion (default: True for safety).

    Returns:
        Deletion result or dry-run report.
    """
    from ..core.operations import FileOperations

    ops = FileOperations()
    path_list = [paths] if isinstance(paths, str) else paths

    if dry_run:
        analysis = await ops.analyze_deletion(path_list)
        return {
            "success": True,
            "dry_run": True,
            "files_to_delete": analysis.file_count,
            "total_size": analysis.total_size,
            "total_size_formatted": format_size(analysis.total_size),
            "warnings": analysis.warnings
        }

    results = await ops.delete_files(
        paths=path_list,
        permanent=permanent
    )

    return {
        "success": results.success,
        "dry_run": False,
        "deleted_count": results.deleted_count,
        "freed_space": results.freed_space,
        "errors": results.errors
    }


@mcp.tool()
async def nexus_organize(
    path: str,
    strategy: str = "category",
    dry_run: bool = True
) -> Dict[str, Any]:
    """
    Organize files in a directory.

    Args:
        path: Directory to organize.
        strategy: Organization strategy (category, date, size, extension).
        dry_run: Only simulate organization (default: True).

    Returns:
        Organization plan or execution result.
    """
    from ..agents.organization import OrganizationAgent

    agent = OrganizationAgent()

    if dry_run:
        plan = await agent.create_plan(path, strategy)
        return {
            "success": True,
            "dry_run": True,
            "strategy": strategy,
            "operations": plan.operations,
            "files_affected": plan.file_count,
            "folders_to_create": plan.new_folders
        }

    result = await agent.execute(path, strategy=strategy)
    return {
        "success": result.success,
        "dry_run": False,
        "files_moved": result.files_moved,
        "folders_created": result.folders_created,
        "errors": result.errors
    }


@mcp.tool()
async def nexus_analyze(
    path: str,
    analysis_type: str = "entropy"
) -> Dict[str, Any]:
    """
    Analyze a file or directory.

    Args:
        path: Path to analyze.
        analysis_type: Type of analysis (entropy, duplicates, large_files, ads).

    Returns:
        Analysis results.
    """
    from ..core.analysis import FileAnalyzer

    analyzer = FileAnalyzer()

    if analysis_type == "entropy":
        result = await analyzer.calculate_entropy(path)
        return {
            "success": True,
            "path": path,
            "entropy": result.entropy,
            "entropy_class": result.classification,
            "is_encrypted": result.entropy > 7.9
        }
    elif analysis_type == "duplicates":
        result = await analyzer.find_duplicates(path)
        return {
            "success": True,
            "duplicate_groups": len(result.groups),
            "total_duplicates": result.total_count,
            "wasted_space": result.wasted_space,
            "groups": result.groups[:20]  # Limit output
        }
    elif analysis_type == "large_files":
        result = await analyzer.find_large_files(path)
        return {
            "success": True,
            "large_files": [
                {
                    "path": f.path,
                    "size": f.size,
                    "size_formatted": format_size(f.size)
                }
                for f in result.files[:50]
            ]
        }
    elif analysis_type == "ads":
        result = await analyzer.scan_ads(path)
        return {
            "success": True,
            "files_with_ads": result.count,
            "streams": result.streams
        }
    else:
        return {
            "success": False,
            "error": f"Unknown analysis type: {analysis_type}"
        }


@mcp.tool()
async def nexus_ai_models(
    action: str = "list"
) -> Dict[str, Any]:
    """
    Manage AI/ML models on the system.

    Args:
        action: Action to perform (list, analyze, move).

    Returns:
        AI model information.
    """
    from ..core.ai_models import AIModelManager

    manager = AIModelManager()

    if action == "list":
        models = await manager.discover()
        return {
            "success": True,
            "model_count": len(models),
            "total_size": sum(m.size for m in models),
            "models": [
                {
                    "name": m.name,
                    "path": m.path,
                    "size": m.size,
                    "size_formatted": format_size(m.size),
                    "provider": m.provider
                }
                for m in models
            ]
        }
    elif action == "analyze":
        analysis = await manager.analyze()
        return {
            "success": True,
            "total_size": analysis.total_size,
            "by_provider": analysis.by_provider,
            "recommendations": analysis.recommendations
        }
    else:
        return {"success": False, "error": f"Unknown action: {action}"}


@mcp.tool()
async def nexus_recommendations() -> Dict[str, Any]:
    """
    Get space recovery recommendations.

    Returns:
        List of recommendations with potential savings.
    """
    from ..core.recommendations import RecommendationEngine

    engine = RecommendationEngine()
    recommendations = await engine.generate()

    return {
        "success": True,
        "count": len(recommendations),
        "total_potential_savings": sum(r.potential_savings for r in recommendations),
        "recommendations": [
            {
                "type": r.type,
                "description": r.description,
                "potential_savings": r.potential_savings,
                "potential_savings_formatted": format_size(r.potential_savings),
                "risk": r.risk,
                "is_reversible": r.is_reversible
            }
            for r in recommendations
        ]
    }


def format_size(size_bytes: int) -> str:
    """Format bytes as human-readable string."""
    for unit in ["B", "KB", "MB", "GB", "TB"]:
        if abs(size_bytes) < 1024:
            return f"{size_bytes:.2f} {unit}"
        size_bytes /= 1024
    return f"{size_bytes:.2f} PB"


# Entry point for running the server
def main():
    """Run the MCP server."""
    mcp.run()


if __name__ == "__main__":
    main()
```

**Next Task:** [6.1.2](#task-612-create-ide-configurations)

---

#### Task 6.1.2: Create IDE Configurations

**Status:** `[ ]` Not Started

**Description:**
Create configuration files for various IDEs.

**Acceptance Criteria:**
- [ ] Claude Code configuration
- [ ] Windsurf configuration
- [ ] Cursor configuration
- [ ] LM Studio configuration

**Files to Create:**
```
configs/claude-code-mcp.json
configs/windsurf-mcp.json
configs/cursor-mcp.json
configs/lm-studio-mcp.json
```

**Implementation:**
```json
// claude-code-mcp.json
{
  "mcpServers": {
    "winhance-fs": {
      "command": "python",
      "args": ["-m", "nexus_agents.mcp"],
      "cwd": "C:/Program Files/Winhance-FS",
      "env": {
        "NEXUS_LOG_LEVEL": "INFO"
      }
    }
  }
}
```

```json
// windsurf-mcp.json (for settings.json)
{
  "mcp": {
    "servers": {
      "winhance-fs": {
        "command": "python",
        "args": ["-m", "nexus_agents.mcp"],
        "cwd": "C:/Program Files/Winhance-FS"
      }
    }
  }
}
```

**Dependencies:** [6.1.1](#task-611-create-fastmcp-server)

**Next Task:** [6.2.1](#task-621-implement-security-controls)

---

### 6.2 Security & Safety

#### Task 6.2.1: Implement Security Controls

**Status:** `[ ]` Not Started

**Description:**
Add security controls for safe file operations.

**Acceptance Criteria:**
- [ ] Path validation (prevent traversal)
- [ ] Protected paths list
- [ ] Dry-run by default for destructive ops
- [ ] Confirmation for risky operations

**Files to Create:**
```
src/nexus-agents/nexus_agents/mcp/security.py
```

**Implementation:**
```python
# security.py
from typing import List, Set
from pathlib import Path
import os

class SecurityGuard:
    """Security controls for MCP operations."""

    # Paths that should never be modified
    PROTECTED_PATHS: Set[str] = {
        "C:\\Windows",
        "C:\\Program Files",
        "C:\\Program Files (x86)",
        "C:\\ProgramData",
        "C:\\Users\\Default",
        "C:\\$Recycle.Bin",
        "C:\\System Volume Information",
    }

    # Extensions that require extra confirmation
    SENSITIVE_EXTENSIONS: Set[str] = {
        ".exe", ".dll", ".sys", ".bat", ".cmd", ".ps1",
        ".reg", ".msi", ".vbs", ".wsf"
    }

    @classmethod
    def validate_path(cls, path: str) -> tuple[bool, str]:
        """Validate a path for safety."""
        try:
            resolved = Path(path).resolve()

            # Check for path traversal
            if ".." in str(path):
                return False, "Path traversal detected"

            # Check against protected paths
            for protected in cls.PROTECTED_PATHS:
                if str(resolved).lower().startswith(protected.lower()):
                    return False, f"Cannot modify protected path: {protected}"

            # Check if path exists
            if not resolved.exists():
                return False, f"Path does not exist: {path}"

            return True, "Path is valid"

        except Exception as e:
            return False, f"Path validation error: {e}"

    @classmethod
    def is_sensitive_file(cls, path: str) -> bool:
        """Check if a file is sensitive."""
        ext = Path(path).suffix.lower()
        return ext in cls.SENSITIVE_EXTENSIONS

    @classmethod
    def check_operation_safety(
        cls,
        operation: str,
        paths: List[str]
    ) -> tuple[bool, str, List[str]]:
        """Check if an operation is safe to perform."""
        warnings = []

        for path in paths:
            valid, message = cls.validate_path(path)
            if not valid:
                return False, message, warnings

            if cls.is_sensitive_file(path):
                warnings.append(f"Sensitive file: {path}")

        if operation == "delete" and not warnings:
            warnings.append("Delete operation - ensure you have backups")

        return True, "Operation is safe", warnings

    @classmethod
    def require_confirmation(cls, operation: str, paths: List[str]) -> bool:
        """Check if operation requires user confirmation."""
        # Always require confirmation for delete
        if operation == "delete":
            return True

        # Require confirmation for sensitive files
        for path in paths:
            if cls.is_sensitive_file(path):
                return True

        # Require confirmation for large operations
        if len(paths) > 100:
            return True

        return False
```

**Dependencies:** [6.1.2](#task-612-create-ide-configurations)

**Next Task:** [6.2.2](#task-622-add-rate-limiting)

---

#### Task 6.2.2: Add Rate Limiting

**Status:** `[ ]` Not Started

**Description:**
Implement rate limiting to prevent abuse.

**Acceptance Criteria:**
- [ ] Per-tool rate limits
- [ ] Configurable limits
- [ ] Graceful handling of limit exceeded

**Files to Create:**
```
src/nexus-agents/nexus_agents/mcp/rate_limit.py
```

**Dependencies:** [6.2.1](#task-621-implement-security-controls)

**Next Task:** [6.3.1](#task-631-add-progress-reporting)

---

### 6.3 Advanced Features

#### Task 6.3.1: Add Progress Reporting

**Status:** `[ ]` Not Started

**Description:**
Implement progress reporting for long-running operations.

**Acceptance Criteria:**
- [ ] Progress updates via MCP notifications
- [ ] Percentage and stage reporting
- [ ] Estimated time remaining

**Files to Create:**
```
src/nexus-agents/nexus_agents/mcp/progress.py
```

**Dependencies:** [6.2.2](#task-622-add-rate-limiting)

**Next Task:** [6.3.2](#task-632-add-streaming-support)

---

#### Task 6.3.2: Add Streaming Support

**Status:** `[ ]` Not Started

**Description:**
Add streaming responses for large result sets.

**Acceptance Criteria:**
- [ ] Streaming search results
- [ ] Chunked scan results
- [ ] Memory-efficient handling

**Dependencies:** [6.3.1](#task-631-add-progress-reporting)

---

## Phase Completion Checklist

- [ ] All 6.1.x tasks complete (MCP Server Core)
- [ ] All 6.2.x tasks complete (Security & Safety)
- [ ] All 6.3.x tasks complete (Advanced Features)
- [ ] Server starts without errors
- [ ] All tools work from Claude Code
- [ ] Security controls prevent unsafe operations
- [ ] IDE configurations tested

---

## Testing the MCP Server

```bash
# Install the package
pip install -e src/nexus-agents

# Run the server directly
python -m nexus_agents.mcp

# Test with MCP Inspector
npx @anthropic/mcp-inspector python -m nexus_agents.mcp
```

---

**[Back to Roadmap](PROJECT_ROADMAP.md)** | **Previous:** [Phase 5](PHASE_5_AI_AGENTS.md) | **Next:** [Phase 7](PHASE_7_TESTING.md)
