param(
  [Parameter(Mandatory)][string]$Action,
  [int]$Index = 0,
  [string]$Value = '',
  [string]$Name = '',
  [switch]$Add
)
# UI Automation driver for Winhance File Manager smoke tests (sandbox folders only).
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms
$AE = [System.Windows.Automation.AutomationElement]
$CT = [System.Windows.Automation.ControlType]
$TS = [System.Windows.Automation.TreeScope]
$p = Get-Process Winhance -ErrorAction Stop | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
$root = $AE::FromHandle($p.MainWindowHandle)

function All($scope, $type) {
  $c = New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, $type)
  return @($scope.FindAll($TS::Descendants, $c))
}
function RowText($item) {
  $texts = All $item $CT::Text
  return ($texts | ForEach-Object { $_.Current.Name }) -join ' | '
}
function Lists { return All $root $CT::DataGrid }
function Rows($l) { return @(All $l $CT::DataItem) + @(All $l $CT::ListItem) }
function Pat($el, $pattern) { $o = $null; if ($el.TryGetCurrentPattern($pattern, [ref]$o)) { return $o }; return $null }

switch ($Action) {
  'edits' { $i = 0; foreach ($e in (All $root $CT::Edit)) { $v = Pat $e ([System.Windows.Automation.ValuePattern]::Pattern); "[$i] $($v.Current.Value)"; $i++ } }
  'setedit' {
    $e = (All $root $CT::Edit)[$Index]
    try { $e.SetFocus() } catch {}
    (Pat $e ([System.Windows.Automation.ValuePattern]::Pattern)).SetValue($Value); "edit[$Index] = $Value"
  }
  'setnamed' {
    # Set the text of the edit box whose accessible name is $Name
    $e = All $root $CT::Edit | Where-Object { $_.Current.Name -eq $Name } | Select-Object -First 1
    if (-not $e) { "NOT FOUND edit '$Name'"; exit 1 }
    try { $e.SetFocus() } catch {}
    (Pat $e ([System.Windows.Automation.ValuePattern]::Pattern)).SetValue($Value); "edit '$Name' = $Value"
  }
  'buttons' { $i = 0; foreach ($b in (All $root $CT::Button)) { "[$i] name='$($b.Current.Name)' help='$($b.Current.HelpText)'"; $i++ } }
  'btnhelp' {
    # Invoke the Nth button whose tooltip (HelpText) equals $Value
    $bs = @(All $root $CT::Button | Where-Object { $_.Current.HelpText -eq $Value })
    if ($bs.Count -le $Index) { "NOT FOUND button help='$Value' #$Index"; exit 1 }
    try { $bs[$Index].SetFocus() } catch {}
    (Pat $bs[$Index] ([System.Windows.Automation.InvokePattern]::Pattern)).Invoke(); "invoked '$Value' #$Index"
  }
  'btnname' {
    $bs = @(All $root $CT::Button | Where-Object { $_.Current.Name -eq $Value })
    if ($bs.Count -le $Index) { "NOT FOUND button '$Value' #$Index"; exit 1 }
    try { $bs[$Index].SetFocus() } catch {}
    (Pat $bs[$Index] ([System.Windows.Automation.InvokePattern]::Pattern)).Invoke(); "invoked '$Value' #$Index"
  }
  'tab' {
    $t = @(All $root $CT::RadioButton) + @(All $root $CT::TabItem) + @(All $root $CT::Button) | Where-Object { $_.Current.Name -eq $Value } | Select-Object -First 1
    if (-not $t) { "NOT FOUND tab '$Value'"; exit 1 }
    $sp = Pat $t ([System.Windows.Automation.SelectionItemPattern]::Pattern)
    if ($sp) { $sp.Select() } else { $ip = Pat $t ([System.Windows.Automation.InvokePattern]::Pattern); if ($ip) { $ip.Invoke() } else { (Pat $t ([System.Windows.Automation.TogglePattern]::Pattern)).Toggle() } }
    "selected tab '$Value'"
  }
  'rows' {
    $l = (Lists)[$Index]
    foreach ($it in (Rows $l)) { RowText $it }
  }
  'select' {
    # Select row(s) in list $Index whose first text cell equals $Value; -Add keeps existing selection
    $l = (Lists)[$Index]
    $hit = $null
    foreach ($it in (Rows $l)) { if ((RowText $it).Split('|')[0].Trim() -eq $Value) { $hit = $it; break } }
    if (-not $hit) { "NOT FOUND row '$Value' in list $Index"; exit 1 }
    $sp = Pat $hit ([System.Windows.Automation.SelectionItemPattern]::Pattern)
    if ($Add) { $sp.AddToSelection() } else { $sp.Select() }
    $hit.SetFocus()
    "selected '$Value' in list $Index"
  }
  'keys' {
    $h = $p.MainWindowHandle
    Add-Type @"
using System; using System.Runtime.InteropServices;
public static class F { [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h); }
"@
    [F]::SetForegroundWindow($h) | Out-Null; Start-Sleep -Milliseconds 300
    [System.Windows.Forms.SendKeys]::SendWait($Value); "sent keys $Value"
  }
  'menu' {
    # Invoke a context-menu item by name (menus are top-level popups, search from desktop)
    $desk = $AE::RootElement
    $c = New-Object System.Windows.Automation.AndCondition(
      (New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, $CT::MenuItem)),
      (New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty, $Value)),
      (New-Object System.Windows.Automation.PropertyCondition($AE::ProcessIdProperty, $p.Id)))
    $mi = $desk.FindFirst($TS::Descendants, $c)
    if (-not $mi) { "NOT FOUND menu item '$Value'"; exit 1 }
    (Pat $mi ([System.Windows.Automation.InvokePattern]::Pattern)).Invoke(); "invoked menu '$Value'"
  }
  'menuitems' {
    $desk = $AE::RootElement
    $c = New-Object System.Windows.Automation.AndCondition(
      (New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, $CT::MenuItem)),
      (New-Object System.Windows.Automation.PropertyCondition($AE::ProcessIdProperty, $p.Id)))
    foreach ($m in $desk.FindAll($TS::Descendants, $c)) { "$($m.Current.Name) [enabled=$($m.Current.IsEnabled)]" }
  }
  'clicktext' {
    # Real left-click at the centre of the first Text element named $Value (for mouse-only controls)
    $t = All $root $CT::Text | Where-Object { $_.Current.Name -eq $Value } | Select-Object -First 1
    if (-not $t) { "NOT FOUND text '$Value'"; exit 1 }
    $r = $t.Current.BoundingRectangle
    Add-Type @"
using System; using System.Runtime.InteropServices;
public static class M {
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint x, uint y, uint d, UIntPtr e);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
}
"@
    [M]::SetForegroundWindow($p.MainWindowHandle) | Out-Null; Start-Sleep -Milliseconds 200
    $x = [int]($r.X + $r.Width / 2); $y = [int]($r.Y + $r.Height / 2)
    [M]::SetCursorPos($x, $y) | Out-Null; Start-Sleep -Milliseconds 80
    [M]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero); [M]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    "clicked text '$Value' at $x,$y"
  }
  'combo' {
    # Select item $Value in the Nth ComboBox (expands it so WPF realizes the items)
    $cb = (All $root $CT::ComboBox)[$Index]
    if (-not $cb) { "NO COMBO #$Index"; exit 1 }
    $ec = Pat $cb ([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
    $ec.Expand(); Start-Sleep -Milliseconds 300
    $item = All $cb $CT::ListItem | Where-Object { $_.Current.Name -eq $Value } | Select-Object -First 1
    if (-not $item) { $names = (All $cb $CT::ListItem | % { $_.Current.Name }) -join ','; $ec.Collapse(); "NO ITEM '$Value' in combo #$Index ($names)"; exit 1 }
    (Pat $item ([System.Windows.Automation.SelectionItemPattern]::Pattern)).Select()
    try { $ec.Collapse() } catch {}
    "combo #$Index = $Value"
  }
  'combos' { $i = 0; foreach ($c in (All $root $CT::ComboBox)) { $v = Pat $c ([System.Windows.Automation.ValuePattern]::Pattern); "[$i] $($c.Current.Name) = $(if($v){$v.Current.Value})"; $i++ } }
  'check' {
    # Set the checkbox whose accessible name is $Name to $Value ('on' or 'off')
    $c = All $root $CT::CheckBox | Where-Object { $_.Current.Name -eq $Name } | Select-Object -First 1
    if (-not $c) { "NOT FOUND checkbox '$Name'"; exit 1 }
    $tp = Pat $c ([System.Windows.Automation.TogglePattern]::Pattern)
    $want = if ($Value -eq 'on') { 'On' } else { 'Off' }
    $n = 0; while ($tp.Current.ToggleState.ToString() -ne $want -and $n -lt 3) { $tp.Toggle(); $n++ }
    "checkbox '$Name' = $($tp.Current.ToggleState)"
  }
  'checks' { foreach ($c in (All $root $CT::CheckBox)) { $tp = Pat $c ([System.Windows.Automation.TogglePattern]::Pattern); "$($c.Current.Name) = $(if($tp){$tp.Current.ToggleState})" } }
  'texts' { foreach ($t in (All $root $CT::Text)) { $n = $t.Current.Name; if ($n -match $Value) { $n } } }
  default { "unknown action $Action"; exit 9 }
}
