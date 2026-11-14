Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'src\Winhance.WPF\Winhance.WPF.csproj'

dotnet run --project $project --configuration Debug
exit $LASTEXITCODE
