# Verify register sync: local bot listener + public mingler.cc tunnel.
$ErrorActionPreference = 'Continue'

$localUrl = 'http://127.0.0.1:3847/api/register/health'
$publicUrl = 'https://sync.mingler.cc/api/register/health'

function Test-RegisterHealth {
    param([string]$Label, [string]$Url)
    Write-Host "`n[$Label] $Url" -ForegroundColor Cyan
    try {
        $r = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 15
        Write-Host "  Status: $($r.StatusCode)" -ForegroundColor Green
        Write-Host "  Body: $($r.Content)"
        return $true
    } catch {
        Write-Host "  FAILED: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

$okLocal = Test-RegisterHealth -Label 'Local bot' -Url $localUrl
$okPublic = Test-RegisterHealth -Label 'Public (tunnel)' -Url $publicUrl

Write-Host ''
if ($okLocal -and $okPublic) {
    Write-Host 'OK — ready for /register link push sync.' -ForegroundColor Green
    exit 0
}
if ($okLocal -and -not $okPublic) {
    Write-Host 'Bot is up locally; tunnel or DNS not ready. Start cloudflared (start-register-sync-tunnel.cmd).' -ForegroundColor Yellow
    exit 1
}
if (-not $okLocal) {
    Write-Host 'Bot not listening on 127.0.0.1:3847 — start The Mingler first.' -ForegroundColor Yellow
    exit 2
}
