# register-sync.ps1 - Push Toontown Companion data to The Mingler (no tunnel).
# Usage (copy register-sync.cmd + this file to the same folder on your game PC):
#   .\register-sync.cmd -SyncUrl "http://BOT:3847/api/register/sync" -Token "paste-from-discord"
# Or: powershell -ExecutionPolicy Bypass -File .\register-sync.ps1 -SyncUrl "..." -Token "..."

param(
    [Parameter(Mandatory = $true)]
    [string]$SyncUrl,

    [Parameter(Mandatory = $true)]
    [string]$Token
)

$ErrorActionPreference = "Stop"

$ProductVersion = "1.0.0"
# ASCII only - em dashes break PowerShell on some Windows encodings when file is copied
$UserAgent = "The Mingler/$ProductVersion (Discord Bot - register-sync) Windows"

# Per-port fetch timeout (matches bot owner pull / companionConfig TRY_FETCH 8s)
$CompanionPortTimeoutSec = 8

$bytes = New-Object byte[] 24
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$CompanionAuth = -join ($bytes | ForEach-Object { $_.ToString("x2") })

$Ports = @(1547, 1548, 1549, 1550, 1551, 1552, 1553, 1554)
$CompanionPayloads = New-Object System.Collections.Generic.List[object]
$OkPorts = New-Object System.Collections.Generic.List[int]
$lastPortError = $null

foreach ($port in $Ports) {
    $uri = "http://127.0.0.1:$port/all.json"
    try {
        $resp = Invoke-WebRequest -Uri $uri -Method GET -UseBasicParsing -TimeoutSec $CompanionPortTimeoutSec `
            -Headers @{
                Host            = "localhost:$port"
                "User-Agent"    = $UserAgent
                Authorization   = $CompanionAuth
                Accept          = "application/json"
            }
        if ($resp.StatusCode -eq 200 -and $resp.Content) {
            $parsed = $resp.Content | ConvertFrom-Json
            if ($parsed) {
                [void]$CompanionPayloads.Add($parsed)
                [void]$OkPorts.Add($port)
            }
        }
    } catch {
        if ($port -eq 1547) {
            $lastPortError = $_.Exception.Message
        }
    }
}

if ($CompanionPayloads.Count -eq 0) {
    Write-Host "Could not reach Companion API on ports $($Ports -join ', ')." -ForegroundColor Red
    Write-Host ""
    Write-Host "This script must run on the SAME PC where Toontown is running." -ForegroundColor Yellow
    Write-Host "Check on that PC:"
    Write-Host "  - Game is open and you are logged into a toon (not just the launcher)"
    Write-Host "  - Settings -> Gameplay -> Miscellaneous -> Companion App Support ON"
    Write-Host "  - You clicked Accept on the in-game Companion prompt"
    if ($lastPortError) {
        Write-Host ""
        Write-Host "Port 1547 error: $lastPortError" -ForegroundColor DarkYellow
    }
    Write-Host ""
    Write-Host "Quick test (run while in-game on this PC):" -ForegroundColor Cyan
    Write-Host '  Invoke-WebRequest -Uri "http://127.0.0.1:1547/all.json" -UseBasicParsing'
    exit 1
}

Write-Host "Companion OK on port(s) $($OkPorts -join ', ') ($($CompanionPayloads.Count) payload(s)). Uploading..." -ForegroundColor Cyan

$bodyObj = @{ companions = $CompanionPayloads.ToArray() }
$body = $bodyObj | ConvertTo-Json -Depth 100 -Compress

try {
    $upload = Invoke-WebRequest -Uri $SyncUrl -Method POST -UseBasicParsing -TimeoutSec 60 `
        -Headers @{
            Authorization  = "Bearer $Token"
            "Content-Type" = "application/json"
        } `
        -Body $body

    Write-Host $upload.Content -ForegroundColor Green
    Write-Host "Done. Check Discord with /register status"
    exit 0
} catch {
    $detail = $_.Exception.Message
    if ($_.Exception.Response) {
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $detail = $reader.ReadToEnd()
        } catch {
            # ignore stream read errors
        }
    }
    Write-Host "Upload failed: $detail" -ForegroundColor Red
    exit 1
}
