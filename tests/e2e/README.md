# Winhance File Manager end-to-end sweep

Drives the real, built app through Windows UI Automation and checks every result on disk.

```powershell
dotnet build Winhance.sln
python tests/e2e/e2e.py sweep3      # any name other than "sweep1" also runs the extended checks
```

- Uses a throwaway sandbox at `C:\Temp\fmtest` (recreated on every run).
- Never clicks anything on the system-tweak pages (Software & Apps, Optimize, Customize, Settings); they are
  only opened and screenshotted. Space Recovery's **Clean Selected** is answered **No**.
- Screenshots and `results.txt` go to `%TEMP%\winhance-e2e\<sweep-name>`.
- Keep hands off the app while it runs; the sweep takes about 10 minutes.

## Input discipline

Field values are always replaced whole (UI Automation `ValuePattern.SetValue`), read back where it matters, and
then acted on. When a check fails, first confirm the field contents and focus the harness actually used — most
apparent UI failures during development were automation timing or input issues, not app defects.

| Helper | Purpose |
|--------|---------|
| `uia.ps1` | Click / focus a named element |
| `uia2.ps1` | Edits, buttons, grid rows, selection, combo boxes, keys, context menus, mouse-only tabs |
| `dlg.ps1` | Drive a WPF dialog by title |
| `msgbox.ps1` | Read or answer a Win32 message box |
| `shot.ps1` | Screenshot the Winhance window |
