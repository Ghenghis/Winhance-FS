# Winhance-FS handoff — 2026-11-14

Branch: `claude/file-manager-release-polish` (local, not pushed). Base: `main` @ `a5300af`.

## State

| Check | Result |
|-------|--------|
| `dotnet build Winhance.sln` | 0 errors, 0 warnings |
| `dotnet test tests/Winhance.Tests` | 147 / 147 pass (incl. DI validation + 5 regression tests) |
| `pytest tests/` (CI marker set) | 74 / 74 pass |
| E2E sweep 3 (`tests/e2e/e2e.py`) | 69 / 69 pass, 0 exceptions logged |
| Rust `src/nexus_core` | **not verified** — `cargo` is not installed on this PC |
| `ruff` (CI lint for Python) | **not run** — `ruff` is not installed on this PC |

## End-to-end sweeps (actual results)

| Sweep | Result | Failures and classification |
|-------|--------|-----------------------------|
| 1 | 44 / 45 | Unicode file name read back garbled — **automation** (PowerShell→Python output encoding). Screenshot showed the app rendered it correctly. Review of screenshots found 9 **app** polish defects (fixed): clipped names, Enter in path box did nothing, no active-pane indicator, no status bar, hidden 5px toolbar column, Batch Rename status not shown, Space Recovery description clipped, counts without separators, rule editors missing for most rule types. |
| 2 | 61 / 67 | Context menu did not open (Shift+F10 raced window activation) and the delete-confirm check that depended on it — **automation** (retry added). Status bar had no copy feedback — **app** (copy/move-to-pane set no message; fixed). Batch Rename preview/apply after an earlier Apply+Undo — **app** (file list cleared after apply and never reloaded; fixed). |
| 3 | 69 / 69 | — |

Defects fixed before the sweeps (found during manual testing): paste moving Explorer-copied files, Organizer
Apply moving nothing, Space Recovery totals/duplicates/actions/drive filter/treemap, Batch Rename typed path and
live preview, Date rule using today's date, unbound Organizer buttons, read-only `SelectedItems` binding.

## Remaining issues (not fixed)

1. **File Manager tab headers are mouse-only** (Borders with mouse bindings): not reachable by keyboard or
   screen readers. Convert to `RadioButton`/`ToggleButton` styled as tabs.
2. **AI model "Relocate"** is reported by Space Recovery but has no button; backend `RelocateModelsAsync` and
   `scripts/relocation/*.ps1` exist.
3. **Not in the UI:** favorites panel (also uses its own JSON store instead of `IFavoritesService`), tab strip,
   search panel, duplicate finder.
4. **Rust and Python lint not verified here** (no `cargo`/`ruff`); CI runs both.
5. Python: Pydantic V2 deprecation warning in `nexus_ai/config.py` (`class Config` → `ConfigDict`).
6. Local `typer`/`click` mismatch prevents running `nexus_cli` on this PC (environment, not repo).
7. `IFileOperationsService` delete is always permanent and the queue's "recycle" delete is permanent — the UI
   does not use them, but anything new that does must not assume Recycle Bin semantics.

## Where things are

- Archived clutter and dead code: `C:\Winhance-FS-archive\2026-11-14\` with `MANIFEST.txt`
  (tracked files are also in git history).
- E2E harness: `tests/e2e/` (see its README). Screenshots: `%TEMP%\winhance-e2e\`.
- Release process: tag `v*` → `.github/workflows/build-release.yml`; local package: `scripts/release.ps1`.
