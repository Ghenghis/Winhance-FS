param(
    [string]$Version = '1.0.0',
    [string]$Runtime = 'win-x64',
    [switch]$SkipTests,
    [switch]$UseLegacyReleaseScript
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot

if ($UseLegacyReleaseScript) {
    $legacyScript = Join-Path $PSScriptRoot 'release-legacy.ps1'
    if (-not (Test-Path -LiteralPath $legacyScript)) {
        Write-Error "Legacy release script not found: $legacyScript"
        exit 1
    }

    & $legacyScript -Version $Version -SkipTests:$SkipTests
    exit $LASTEXITCODE
}

$solution = Join-Path $repoRoot 'Winhance.sln'
$project = Join-Path $repoRoot 'src\Winhance.WPF\Winhance.WPF.csproj'
$publishDir = Join-Path $repoRoot 'artifacts\publish\Winhance.WPF'
$versionTag = if ($Version.StartsWith('v', [System.StringComparison]::OrdinalIgnoreCase)) { $Version } else { "v$Version" }
$versionNumber = $versionTag.Substring(1)
$assemblyVersionBase = ($versionNumber -split '-', 2)[0]
$assemblyVersionParts = @($assemblyVersionBase -split '\.')
while ($assemblyVersionParts.Count -lt 4) {
    $assemblyVersionParts += '0'
}
if ($assemblyVersionParts.Count -gt 4) {
    $assemblyVersionParts = $assemblyVersionParts[0..3]
}
$assemblyVersion = [string]::Join('.', $assemblyVersionParts)
$releaseDir = Join-Path $repoRoot ("artifacts\release\{0}" -f $versionTag)
$archivePath = Join-Path $releaseDir ("Winhance-FS-{0}-{1}-Portable.zip" -f $versionTag, $Runtime)
$checksumPath = "$archivePath.sha256"

if (-not $SkipTests) {
    dotnet test $solution --configuration Release --nologo --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

dotnet publish $project `
    --configuration Release `
    --runtime $Runtime `
    --self-contained false `
    --output $publishDir `
    --nologo `
    -p:Version=$versionNumber `
    -p:AssemblyVersion=$assemblyVersion `
    -p:FileVersion=$assemblyVersion `
    -p:InformationalVersion=$versionNumber
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}
if (Test-Path -LiteralPath $checksumPath) {
    Remove-Item -LiteralPath $checksumPath -Force
}

Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $archivePath -Force

if (-not (Test-Path -LiteralPath $archivePath)) {
    Write-Error "Release archive was not produced: $archivePath"
    exit 1
}

$hash = Get-FileHash -LiteralPath $archivePath -Algorithm SHA256
$archiveName = Split-Path -Leaf $archivePath
Set-Content -Path $checksumPath -Value "$($hash.Hash)  $archiveName"

Write-Host "Release archive produced: $archivePath"
Write-Host "SHA256 produced: $checksumPath"
exit 0
