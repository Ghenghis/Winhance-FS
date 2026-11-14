"""Default storage locations for NexusFS.

Everything lives under ``NEXUSFS_HOME`` when that environment variable is set, otherwise under
``%LOCALAPPDATA%\\NexusFS`` on Windows (``~/.nexusfs`` elsewhere). No drive letter is assumed.
"""

from __future__ import annotations

import os
from pathlib import Path


def nexus_home() -> Path:
    """Root folder for NexusFS data, configuration, logs and backups."""
    override = os.environ.get("NEXUSFS_HOME")
    if override:
        return Path(override)
    local_app_data = os.environ.get("LOCALAPPDATA")
    if local_app_data:
        return Path(local_app_data) / "NexusFS"
    return Path.home() / ".nexusfs"


def repo_root() -> Path:
    """The Winhance-FS repository root (this file is src/nexus_ai/paths.py)."""
    return Path(__file__).resolve().parents[2]
