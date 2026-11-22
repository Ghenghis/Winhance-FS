param([string]$Out = (Join-Path $env:TEMP 'winhance-shot.png'))
# Capture the Winhance main window (brings it to the foreground first).
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class W {
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int c);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
}
"@
$p = Get-Process Winhance -ErrorAction Stop | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
$h = $p.MainWindowHandle
[W]::ShowWindow($h, 9) | Out-Null
[W]::SetForegroundWindow($h) | Out-Null
$r = New-Object W+RECT
[W]::GetWindowRect($h, [ref]$r) | Out-Null
# Park the mouse in the bottom-right corner so no hover highlight ends up in the picture, then let
# hover effects and dialog fade-in animations finish before capturing.
[W]::SetCursorPos($r.R - 12, $r.B - 12) | Out-Null
Start-Sleep -Milliseconds 1200
$w = $r.R - $r.L; $hgt = $r.B - $r.T
$bmp = New-Object System.Drawing.Bitmap $w, $hgt
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($r.L, $r.T, 0, 0, $bmp.Size)
$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
"$($p.MainWindowTitle) ${w}x${hgt} -> $Out"
