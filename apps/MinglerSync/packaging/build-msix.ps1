param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',

    [ValidateSet('win-x64')]
    [string]$Runtime = 'win-x64',
    [string]$DotNetPath = 'dotnet',
    [string]$OutputDirectory = ''
)

$ErrorActionPreference = 'Stop'
$env:WINAPP_CLI_TELEMETRY_OPTOUT = '1'

$packagingDir = $PSScriptRoot
$projectRoot = Split-Path -Parent $packagingDir
$projectFile = Join-Path $projectRoot 'src\MinglerSync.WinForms\MinglerSync.WinForms.csproj'
$manifestSource = Join-Path $packagingDir 'Package.appxmanifest'
$assetsSource = Join-Path $packagingDir 'Assets'
[xml]$manifest = Get-Content -LiteralPath $manifestSource -Raw
[xml]$project = Get-Content -LiteralPath $projectFile -Raw
$version = [string]$manifest.Package.Identity.Version
$fileVersion = [string]($project.Project.PropertyGroup | Where-Object FileVersion).FileVersion
if ($version -ne $fileVersion) { throw 'App and package file versions must match.' }
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $projectRoot "artifacts\candidate-$version" }
$outputRoot = [System.IO.Path]::GetFullPath($OutputDirectory)
$layoutDir = Join-Path $outputRoot 'layout'
$outputPackage = Join-Path $outputRoot "MinglerSync_${version}_x64.msix"

if (-not (Get-Command winapp -ErrorAction SilentlyContinue)) {
    throw 'Microsoft winapp CLI is required. Install Microsoft.WinAppCli through winget.'
}

# Never delete or overwrite an existing build or failed receipt.
if (Test-Path -LiteralPath $outputRoot) { throw "Output directory already exists; choose a fresh directory: $outputRoot" }
if (-not (Test-Path -LiteralPath $assetsSource)) {
    # Assets are generated (gitignored); support a clean source checkout too.
    $iconSource = Join-Path $packagingDir 'mingler-bot-icon.png'
    if (-not (Test-Path -LiteralPath $iconSource)) { throw 'Public icon source is missing.' }
    winapp manifest update-assets $iconSource --manifest $manifestSource
    if ($LASTEXITCODE -ne 0) { throw 'Package asset generation failed.' }
    [xml]$generatedManifest = Get-Content -LiteralPath $manifestSource -Raw
    if ([string]$generatedManifest.Package.Identity.Version -ne $version) { throw 'Asset generation changed package version.' }
}
New-Item -ItemType Directory -Path $outputRoot -ErrorAction Stop | Out-Null

& $DotNetPath publish $projectFile `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -o $layoutDir

if ($LASTEXITCODE -ne 0) {
    throw 'dotnet publish failed.'
}

Copy-Item -LiteralPath $assetsSource -Destination (Join-Path $layoutDir 'Assets') -Recurse

# No certificate flags: unsigned Store-submission candidate only.
winapp package $layoutDir `
    --manifest $manifestSource `
    --exe 'Mingler Sync.exe' `
    --output $outputPackage

if ($LASTEXITCODE -ne 0) {
    throw 'MSIX packaging failed.'
}

Get-FileHash -LiteralPath $outputPackage -Algorithm SHA256
Write-Host "Created unsigned candidate: $outputPackage"
