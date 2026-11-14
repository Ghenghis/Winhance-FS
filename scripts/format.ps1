Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot

dotnet format (Join-Path $repoRoot 'Winhance.sln')
exit $LASTEXITCODE
