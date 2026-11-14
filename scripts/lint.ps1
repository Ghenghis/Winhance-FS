Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot 'Winhance.sln'

dotnet format $solution --verify-no-changes --verbosity diagnostic
if ($LASTEXITCODE -ne 0) {
    Write-Error 'Format violations detected'
    exit 1
}

dotnet build $solution --nologo /p:TreatWarningsAsErrors=true
exit $LASTEXITCODE
