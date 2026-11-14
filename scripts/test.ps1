param(
    [switch]$Coverage
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$dotnetArgs = @(
    'test',
    (Join-Path $repoRoot 'Winhance.sln'),
    '--nologo',
    '--logger',
    'console;verbosity=normal'
)

if ($Coverage) {
    $dotnetArgs += @('--collect', 'XPlat Code Coverage')
}

dotnet @dotnetArgs
exit $LASTEXITCODE
