param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',

    [string]$Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'
$env:WINAPP_CLI_TELEMETRY_OPTOUT = '1'

$packagingDir = $PSScriptRoot
$projectRoot = Split-Path -Parent $packagingDir
$projectFile = Join-Path $projectRoot 'src\MinglerSync.WinForms\MinglerSync.WinForms.csproj'
$manifestSource = Join-Path $packagingDir 'Package.appxmanifest'
$iconSource = Join-Path $packagingDir 'mingler-bot-icon.png'
$assetsSource = Join-Path $packagingDir 'Assets'
$artifactDir = Join-Path $projectRoot 'artifacts'
$layoutDir = Join-Path $artifactDir 'msix-layout'
$outputPackage = Join-Path $artifactDir 'MinglerSync_1.0.0.0_x64.msix'

if (-not (Get-Command winapp -ErrorAction SilentlyContinue)) {
    throw 'Microsoft winapp CLI is required. Install Microsoft.WinAppCli through winget.'
}

if (-not (Test-Path -LiteralPath $iconSource)) {
    throw 'The Mingler bot icon source is missing.'
}

winapp manifest update-assets $iconSource --manifest $manifestSource
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $assetsSource)) {
    throw 'MSIX asset generation failed.'
}

$artifactRoot = [System.IO.Path]::GetFullPath($artifactDir).TrimEnd('\') + '\'
$resolvedLayout = [System.IO.Path]::GetFullPath($layoutDir)
if (-not $resolvedLayout.StartsWith($artifactRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Refusing to clean an MSIX layout outside the project artifacts directory.'
}

if (Test-Path -LiteralPath $layoutDir) {
    Remove-Item -LiteralPath $layoutDir -Recurse -Force
}

if (Test-Path -LiteralPath $outputPackage) {
    Remove-Item -LiteralPath $outputPackage -Force
}

dotnet publish $projectFile `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -o $layoutDir

if ($LASTEXITCODE -ne 0) {
    throw 'dotnet publish failed.'
}

Copy-Item -LiteralPath $assetsSource -Destination (Join-Path $layoutDir 'Assets') -Recurse -Force

winapp package $layoutDir `
    --manifest $manifestSource `
    --exe 'Mingler Sync.exe' `
    --output $outputPackage

if ($LASTEXITCODE -ne 0) {
    throw 'MSIX packaging failed.'
}

Write-Host "Created $outputPackage"
