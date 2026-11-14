Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$checks = New-Object System.Collections.Generic.List[object]

function Add-Check {
    param(
        [string]$Name,
        [bool]$Passed,
        [string]$Detail
    )

    $script:checks.Add([pscustomobject]@{
        Name = $Name
        Passed = $Passed
        Detail = $Detail
    })
}

function Test-CommandAvailable {
    param([string]$Name)
    return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

$psVersion = $PSVersionTable.PSVersion
Add-Check -Name 'PowerShell 7+' -Passed ($psVersion.Major -ge 7) -Detail $psVersion.ToString()

$dotnetAvailable = Test-CommandAvailable 'dotnet'
if ($dotnetAvailable) {
    $sdks = @(dotnet --list-sdks 2>$null)
    $hasSdk9 = $sdks | Where-Object { $_ -match '^9\.0\.' }
    Add-Check -Name '.NET SDK 9.0.x' -Passed ([bool]$hasSdk9) -Detail (($sdks -join '; ') -replace '\s+', ' ')

    $sources = @(dotnet nuget list source 2>$null)
    $enabledSource = $sources | Where-Object { $_ -match '^\s*\d+\.\s+' }
    Add-Check -Name 'NuGet sources configured' -Passed ([bool]$enabledSource) -Detail (($sources -join ' ') -replace '\s+', ' ')
} else {
    Add-Check -Name '.NET CLI' -Passed $false -Detail 'dotnet was not found on PATH'
    Add-Check -Name '.NET SDK 9.0.x' -Passed $false -Detail 'dotnet was not found on PATH'
    Add-Check -Name 'NuGet sources configured' -Passed $false -Detail 'dotnet was not found on PATH'
}

$gitAvailable = Test-CommandAvailable 'git'
Add-Check -Name 'Git available' -Passed $gitAvailable -Detail ($(if ($gitAvailable) { (git --version) } else { 'git was not found on PATH' }))
Add-Check -Name 'Winhance.sln present' -Passed (Test-Path -LiteralPath (Join-Path $repoRoot 'Winhance.sln')) -Detail (Join-Path $repoRoot 'Winhance.sln')

$failed = $false
foreach ($check in $checks) {
    $status = if ($check.Passed) { 'PASS' } else { 'FAIL' }
    if (-not $check.Passed) { $failed = $true }
    Write-Host ("[{0}] {1} - {2}" -f $status, $check.Name, $check.Detail)
}

if ($failed) {
    exit 1
}

exit 0
