# The Mingler register-sync — read local Companion data, upload once to the bot.
# Run on the SAME PC as the game. URL + token from /register link only. See README.txt.

param(
    [Parameter(Mandatory = $true)]
    [string]$SyncUrl,

    [Parameter(Mandatory = $true)]
    [string]$Token
)

$ErrorActionPreference = "Stop"
$SyncUrl = $SyncUrl.Trim()
$Token = $Token.Trim()

$ProductVersion = "1.3.0"
$UploadErrPatternToken = "401|unauthorized|invalid.*token|expired"
$UploadErrPatternForbidden = "403|forbidden"
$UploadErrPatternImport = "422|import failed|no toons"
$UploadErrPatternPayload = "413|payload too large|payload_too_large|body too large|request entity too large"
$UploadErrPatternServer = "500|502|503|504|internal server error|bad gateway|service unavailable|gateway timeout"
$UploadErrPatternNetwork = @(
    "unable to connect"
    "connection refused"
    "actively refused"
    "no such host"
    "timed out"
    "timeout"
    "name or service not known"
    "network is unreachable"
    "host unreachable"
    "could not be resolved"
    "unreachable"
) -join "|"

function Get-SyncUrlDisplayHost {
    param([string]$Url)
    $parsedUri = $null
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    $parsedUri = [Uri]$Url
    $ErrorActionPreference = $prevEap
    if (-not $parsedUri -or -not $parsedUri.Host) { return '(from your /register link)' }
    $port = $parsedUri.Port
    $showPort = $port -gt 0 -and $port -ne 80 -and $port -ne 443
    if ($showPort) {
        return ($parsedUri.Scheme + '://' + $parsedUri.Host + ':' + $port)
    }
    return ($parsedUri.Scheme + '://' + $parsedUri.Host)
}

function Get-RegisterSyncUserAgent {
    param([string]$Version)
    $platform = 'Windows'
    if ($PSVersionTable.PSEdition -eq 'Core') {
        if ($IsMacOS) { $platform = 'macOS' }
        elseif ($IsLinux) { $platform = 'Linux' }
        elseif ($IsWindows) { $platform = 'Windows' }
        else { $platform = 'pwsh' }
    }
    return "The Mingler/$Version (Discord Bot - register-sync) $platform"
}

function Test-InternetSyncUrl {
    param([string]$Url)
    $parsedUri = $null
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    $parsedUri = [Uri]$Url
    $ErrorActionPreference = $prevEap
    return ($parsedUri -and $parsedUri.Scheme -eq 'https')
}

function Get-UploadFailureDisplay {
    param(
        [string]$Detail,
        [string]$SyncUrl = ''
    )
    $internetSync = Test-InternetSyncUrl $SyncUrl
    $d = if ($Detail) { $Detail.ToLowerInvariant() } else { "" }
    if ($d -match $UploadErrPatternToken) {
        return 'Links expire and tokens are one-time use. Run /register link in Discord again, then re-run this script.'
    }
    if ($d -match $UploadErrPatternForbidden) {
        if ($internetSync) {
            return 'The server rejected this request. Run /register link again and re-run this script. Turn off VPN if you use one.'
        }
        return 'The server rejected this request (often wrong Wi-Fi or LAN-only sync). Use the same home network as the bot PC and turn off VPN.'
    }
    if ($d -match $UploadErrPatternImport) {
        return 'The server got data but could not import toons. Stay logged in-game, accept the Companion prompt, then run /register link again.'
    }
    if ($d -match $UploadErrPatternPayload) {
        return 'The upload was too large. Ask the bot owner, or sync with fewer toons logged in at once.'
    }
    if ($d -match $UploadErrPatternServer) {
        return 'The bot server returned a temporary error. Wait a minute, run /register link again, and re-run this script.'
    }
    if ($d -match $UploadErrPatternNetwork) {
        if ($internetSync) {
            return 'Could not reach the bot server. Check your internet connection, turn off VPN, and confirm /register link is fresh.'
        }
        return 'Could not reach the bot server. Check same Wi-Fi/LAN as the bot PC, turn off VPN, and confirm the -SyncUrl is reachable.'
    }
    if ($Detail -and $Detail.Trim()) {
        return $Detail.Trim()
    }
    return 'Run /register link in Discord and re-run this script.'
}

function Get-CompanionToonLabel {
    param([object]$Parsed)
    if (-not $Parsed) { return $null }
    if ($Parsed.toon -and $Parsed.toon.name) { return [string]$Parsed.toon.name }
    if ($Parsed.name) { return [string]$Parsed.name }
    if ($Parsed.sessions) {
        $names = @($Parsed.sessions | ForEach-Object {
            if ($_.toon -and $_.toon.name) { [string]$_.toon.name }
        } | Where-Object { $_ })
        if ($names.Count -gt 0) { return ($names -join ", ") }
    }
    return $null
}

function Get-WebErrorBody {
    param($ErrorRecord)
    if (-not $ErrorRecord) { return $null }
    $detail = $ErrorRecord.Exception.Message
    if ($ErrorRecord.Exception.Response) {
        $stream = $null
        $prevEap = $ErrorActionPreference
        $ErrorActionPreference = 'SilentlyContinue'
        $stream = $ErrorRecord.Exception.Response.GetResponseStream()
        if ($stream) {
            $reader = New-Object System.IO.StreamReader($stream)
            $detail = $reader.ReadToEnd()
            $reader.Dispose()
        }
        $ErrorActionPreference = $prevEap
    }
    return $detail
}

function Write-UploadSuccessMessage {
    param([string]$Content)
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    $result = $Content | ConvertFrom-Json
    $ErrorActionPreference = $prevEap

    if (-not ($result -and $result.ok -eq $true)) {
        Write-Host $Content -ForegroundColor Green
        return
    }

    if ($result.message) {
        Write-Host $result.message -ForegroundColor Green
    } elseif ($null -ne $result.syncedToons) {
        Write-Host "Registered $($result.syncedToons) toon(s)." -ForegroundColor Green
    }

    if ($result.toons) {
        foreach ($t in @($result.toons)) {
            $tag = if ($t.wasNew) { ' (new)' } else { '' }
            Write-Host ""
            Write-Host "  $($t.name)$tag" -ForegroundColor Green
            $areas = @($t.updated)
            if ($areas.Count -gt 0) {
                foreach ($area in $areas) {
                    Write-Host "    - $area" -ForegroundColor Gray
                }
            } else {
                Write-Host '    (already up to date)' -ForegroundColor DarkGray
            }
        }
    }
}

function Read-CompanionPortPayload {
    param(
        [int]$Port,
        [string]$UserAgent,
        [string]$CompanionAuth,
        [int]$TimeoutSec
    )
    $uri = "http://127.0.0.1:$Port/all.json"
    $companionHeaders = @{
        Host          = "localhost:$Port"
        "User-Agent"  = $UserAgent
        Authorization = $CompanionAuth
        Accept        = "application/json"
    }
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    $resp = Invoke-WebRequest -Uri $uri -Method GET -UseBasicParsing -TimeoutSec $TimeoutSec -Headers $companionHeaders
    $portError = $null
    if ($Error.Count -gt 0) {
        $portError = $Error[0].Exception.Message
        $Error.Clear()
    }
    $ErrorActionPreference = $prevEap
    if ($resp -and $resp.StatusCode -eq 200 -and $resp.Content) {
        $parsed = $null
        $prevEapJson = $ErrorActionPreference
        $ErrorActionPreference = 'SilentlyContinue'
        $parsed = $resp.Content | ConvertFrom-Json
        $ErrorActionPreference = $prevEapJson
        if ($parsed) {
            return @{ Ok = $true; Parsed = $parsed; Error = $null }
        }
    }
    return @{ Ok = $false; Parsed = $null; Error = $portError }
}

function Send-RegisterSyncUpload {
    param(
        [string]$SyncUrl,
        [string]$Token,
        [string]$Body
    )
    $uploadHeaders = @{
        Authorization  = "Bearer $Token"
        "Content-Type" = "application/json"
    }
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    $uploadResponse = Invoke-WebRequest -Uri $SyncUrl -Method POST -UseBasicParsing -TimeoutSec 60 -Headers $uploadHeaders -Body $Body
    $uploadError = $null
    if ($Error.Count -gt 0) {
        $uploadError = Get-WebErrorBody -ErrorRecord $Error[0]
        $Error.Clear()
    }
    $ErrorActionPreference = $prevEap
    if ($uploadResponse) {
        return @{ Ok = $true; Content = $uploadResponse.Content; Detail = $null }
    }
    return @{ Ok = $false; Content = $null; Detail = $uploadError }
}

function Invoke-RegisterSyncMain {
    $UserAgent = Get-RegisterSyncUserAgent -Version $ProductVersion
    $CompanionPortTimeoutSec = 8
    $Ports = @(1547, 1548, 1549, 1550, 1551, 1552, 1553, 1554)

    Write-Host ""
    Write-Host "The Mingler register-sync v$ProductVersion" -ForegroundColor Cyan
    Write-Host "Step 1 of 2: Read toon data locally (127.0.0.1 only)" -ForegroundColor Cyan
    Write-Host "Game open, Companion App Support ON. Idle ports may take up to ~$($Ports.Count * $CompanionPortTimeoutSec)s." -ForegroundColor DarkGray
    Write-Host ""

    $bytes = New-Object byte[] 24
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $CompanionAuth = -join ($bytes | ForEach-Object { $_.ToString("x2") })

    $CompanionPayloads = New-Object System.Collections.Generic.List[object]
    $lastPortError = $null

    $portScanActivity = "Step 1 of 2: Reading local Companion API"
    $portScanTotal = $Ports.Count
    $portScanIndex = 0
    foreach ($port in $Ports) {
        $portScanIndex++
        $progressPct = [int](($portScanIndex - 1) / $portScanTotal * 100)
        $progressStatus = "Port $port ($portScanIndex of $portScanTotal)"
        Write-Progress -Activity $portScanActivity -Status $progressStatus -PercentComplete $progressPct
        $portResult = Read-CompanionPortPayload -Port $port -UserAgent $UserAgent -CompanionAuth $CompanionAuth -TimeoutSec $CompanionPortTimeoutSec
        if ($portResult.Ok) {
            [void]$CompanionPayloads.Add($portResult.Parsed)
            $label = Get-CompanionToonLabel $portResult.Parsed
            if ($label) {
                Write-Host "  Found: $label (port $port)" -ForegroundColor Green
            } else {
                Write-Host "  Found toon data on port $port" -ForegroundColor Green
            }
        } elseif ($port -eq 1547 -and $portResult.Error) {
            $lastPortError = $portResult.Error
        }
    }
    Write-Progress -Activity $portScanActivity -Completed

    if ($CompanionPayloads.Count -eq 0) {
        Write-Host "Could not reach Companion API on ports $($Ports -join ', ')." -ForegroundColor Red
        Write-Host ""
        Write-Host "Run this on the PC where the game is open:" -ForegroundColor Yellow
        Write-Host '  - Logged into a toon, Companion App Support ON, consent accepted'
        if ($lastPortError) {
            Write-Host ""
            Write-Host "Details (port 1547): $lastPortError" -ForegroundColor DarkYellow
        }
        Write-Host ""
        Write-Host "No data was sent. Run /register link again after fixing the above." -ForegroundColor Cyan
        exit 1
    }

    Write-Host ""
    $syncHost = Get-SyncUrlDisplayHost $SyncUrl
    Write-Host "Step 2 of 2: Send toon data to The Mingler" -ForegroundColor Cyan
    Write-Host "  Destination: $syncHost" -ForegroundColor Gray
    Write-Host "Uploading $($CompanionPayloads.Count) toon profile(s)..." -ForegroundColor Cyan

    $bodyObj = @{ companions = $CompanionPayloads.ToArray() }
    $body = $bodyObj | ConvertTo-Json -Depth 100 -Compress

    $uploadResult = Send-RegisterSyncUpload -SyncUrl $SyncUrl -Token $Token -Body $body
    if ($uploadResult.Ok) {
        Write-UploadSuccessMessage -Content $uploadResult.Content
        Write-Host ""
        Write-Host "Done. Run /register status in Discord to confirm." -ForegroundColor Cyan
        exit 0
    }

    Write-Host ('Upload failed: ' + (Get-UploadFailureDisplay -Detail $uploadResult.Detail -SyncUrl $SyncUrl)) -ForegroundColor Red
    exit 1
}

Invoke-RegisterSyncMain
