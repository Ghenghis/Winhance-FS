param([Parameter(Mandatory)][string]$Title, [string]$SetText = $null, [string]$Click = $null, [switch]$Read)
# Drive a top-level dialog window (by exact title) owned by Winhance via UI Automation.
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
$AE = [System.Windows.Automation.AutomationElement]
$CT = [System.Windows.Automation.ControlType]
$TS = [System.Windows.Automation.TreeScope]
$pid0 = (Get-Process Winhance | Select-Object -First 1).Id
$cond = New-Object System.Windows.Automation.AndCondition(
  (New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty, $Title)),
  (New-Object System.Windows.Automation.PropertyCondition($AE::ProcessIdProperty, $pid0)))
$dlg = $null
for ($i = 0; $i -lt 20 -and -not $dlg; $i++) { $dlg = $AE::RootElement.FindFirst($TS::Descendants, $cond); if (-not $dlg) { Start-Sleep -Milliseconds 250 } }
if (-not $dlg) { "NO DIALOG '$Title'"; exit 1 }
function All($t) { @($dlg.FindAll($TS::Descendants, (New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, $t)))) }
if ($Read) { foreach ($t in (All $CT::Text)) { "text: $($t.Current.Name)" }; foreach ($e in (All $CT::Edit)) { $v = $null; [void]$e.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$v); "edit: $($v.Current.Value)" } }
if ($SetText -ne $null -and $SetText -ne '') {
  $e = (All $CT::Edit)[0]; $v = $null
  [void]$e.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$v); $v.SetValue($SetText); "set text '$SetText'"
}
if ($Click) {
  $b = All $CT::Button | Where-Object { $_.Current.Name -eq $Click } | Select-Object -First 1
  if (-not $b) { "NO BUTTON '$Click'"; exit 1 }
  $ip = $null; [void]$b.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$ip); $ip.Invoke(); "clicked '$Click'"
}
