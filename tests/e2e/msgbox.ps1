param([Parameter(Mandatory)][string]$Title, [ValidateSet('Yes','No','OK','Cancel','Read')][string]$Button = 'Read')
# Press a button on a Win32 MessageBox (#32770) with the exact title, or read its text.
Add-Type @"
using System; using System.Text; using System.Runtime.InteropServices;
public static class MB {
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr FindWindow(string c, string t);
  [DllImport("user32.dll")] public static extern IntPtr GetDlgItem(IntPtr h, int id);
  [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, int m, IntPtr w, IntPtr l);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
}
"@
$h = [IntPtr]::Zero
for ($i = 0; $i -lt 20 -and $h -eq [IntPtr]::Zero; $i++) { $h = [MB]::FindWindow('#32770', $Title); if ($h -eq [IntPtr]::Zero) { Start-Sleep -Milliseconds 250 } }
if ($h -eq [IntPtr]::Zero) { "NO MESSAGEBOX '$Title'"; exit 1 }
$sb = New-Object System.Text.StringBuilder 512
[void][MB]::GetWindowText([MB]::GetDlgItem($h, 0xFFFF), $sb, 512)
"message: $($sb.ToString())"
if ($Button -eq 'Read') { exit 0 }
$ids = @{ OK = 1; Cancel = 2; Yes = 6; No = 7 }
$btn = [MB]::GetDlgItem($h, $ids[$Button])
if ($btn -eq [IntPtr]::Zero) { "NO BUTTON $Button"; exit 1 }
[void][MB]::SendMessage($h, 0x0111, [IntPtr]$ids[$Button], $btn)  # WM_COMMAND(id) to the dialog
"pressed $Button"
