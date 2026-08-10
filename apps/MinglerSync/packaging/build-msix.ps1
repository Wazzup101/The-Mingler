param([ValidateSet('Release', 'Debug')][string]$Configuration = 'Release', [string]$Runtime = 'win-x64')
$ErrorActionPreference = 'Stop'
$env:WINAPP_CLI_TELEMETRY_OPTOUT = '1'
$packagingDir = $PSScriptRoot
$projectRoot = Split-Path -Parent $packagingDir
$manifest = Join-Path $packagingDir 'Package.appxmanifest'
$logo = Join-Path $packagingDir 'mingler-sync-logo.svg'
$assets = Join-Path $packagingDir 'Assets'
$artifacts = Join-Path $projectRoot 'artifacts'
$layout = Join-Path $artifacts 'msix-layout'
$package = Join-Path $artifacts 'MinglerSync_1.0.0.0_x64.msix'
if (-not (Get-Command winapp -ErrorAction SilentlyContinue)) { throw 'Install Microsoft WinApp CLI through winget first.' }
winapp manifest update-assets $logo --manifest $manifest
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $assets)) { throw 'Store asset generation failed.' }
$root = [IO.Path]::GetFullPath($artifacts).TrimEnd('\') + '\'
$resolved = [IO.Path]::GetFullPath($layout)
if (-not $resolved.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe package layout path.' }
if (Test-Path -LiteralPath $layout) { Remove-Item -LiteralPath $layout -Recurse -Force }
if (Test-Path -LiteralPath $package) { Remove-Item -LiteralPath $package -Force }
dotnet publish (Join-Path $projectRoot 'src\MinglerSync.WinForms\MinglerSync.WinForms.csproj') -c $Configuration -r $Runtime --self-contained true -p:PublishSingleFile=true -o $layout
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }
Copy-Item -LiteralPath $assets -Destination (Join-Path $layout 'Assets') -Recurse -Force
winapp package $layout --manifest $manifest --exe 'Mingler Sync.exe' --output $package
if ($LASTEXITCODE -ne 0) { throw 'MSIX packaging failed.' }
Write-Host "Created $package"

