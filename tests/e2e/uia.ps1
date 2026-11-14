param(
  [Parameter(Mandatory)][ValidateSet('click','list','focus')][string]$Action,
  [string]$Name = '',
  [string]$Under = ''
)
# Minimal UI Automation helper for the Winhance window.
#  click <Name>  : invoke/select/click the first element whose Name matches exactly
#  list  [Under] : list named descendants (optionally under the first element named $Under)
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
$p = Get-Process Winhance -ErrorAction Stop | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
$root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
$TS = [System.Windows.Automation.TreeScope]::Descendants

function Find-ByName($scope, $n) {
  $c = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $n)
  return $scope.FindFirst($TS, $c)
}

if ($Action -eq 'list') {
  $scope = $root
  if ($Under) { $scope = Find-ByName $root $Under; if (-not $scope) { "NOT FOUND: $Under"; exit 1 } }
  $all = $scope.FindAll($TS, [System.Windows.Automation.Condition]::TrueCondition)
  $seen = @{}
  foreach ($e in $all) {
    $n = $e.Current.Name; $t = $e.Current.ControlType.ProgrammaticName -replace 'ControlType\.', ''
    if ($n -and $n.Length -lt 90) {
      $k = "$t|$n"
      if (-not $seen[$k]) { $seen[$k] = 1; "{0,-12} {1}  [{2}]" -f $t, $n, ($e.Current.IsEnabled) }
    }
  }
  exit 0
}

$el = Find-ByName $root $Name
if (-not $el) { "NOT FOUND: $Name"; exit 1 }
if ($Action -eq 'focus') { $el.SetFocus(); "focused $Name"; exit 0 }
$pat = $null
if ($el.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$pat)) { $pat.Invoke(); "invoked $Name"; exit 0 }
if ($el.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$pat)) { $pat.Select(); "selected $Name"; exit 0 }
if ($el.TryGetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern, [ref]$pat)) { "REFUSING to toggle $Name"; exit 2 }
"no actionable pattern on $Name"; exit 3
