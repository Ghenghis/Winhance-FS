param(
    [switch]$Release
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$config = if ($Release) { 'Release' } else { 'Debug' }

dotnet build (Join-Path $repoRoot 'Winhance.sln') --configuration $config --nologo
exit $LASTEXITCODE
