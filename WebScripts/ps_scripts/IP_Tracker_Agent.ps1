# Universal IP Tracker - Windows PowerShell 5.1 Agent
$ErrorActionPreference = 'Stop'

$API_URL = "https://iptracker.up.railway.app/update.php"
$TOKEN = "9e81702d0c307325093a9027f8ed59b2fea22f3169b91a82670b3e479cd6975e"
$MAX_IP_ATTEMPTS = 20
$IP_RETRY_SECONDS = 30

$ScriptPath = $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($ScriptPath)) {
    $ScriptPath = $PSCommandPath
}
if ([string]::IsNullOrWhiteSpace($ScriptPath)) {
    $ScriptPath = Join-Path -Path (Get-Location) -ChildPath 'agent.ps1'
}
$LogPath = [System.IO.Path]::ChangeExtension($ScriptPath, '.log')

function Write-Log {
    param([string]$Message)
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    Add-Content -Path $script:LogPath -Value "[$timestamp] $Message"
}

function Initialize-Log {
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    Set-Content -Path $script:LogPath -Value "[$timestamp] Starting..."
}

function Get-TrackedIPv4 {
    $route = Get-NetRoute -AddressFamily IPv4 -DestinationPrefix '0.0.0.0/0' -ErrorAction SilentlyContinue |
        Where-Object {
            $_.State -eq 'Alive' -and
            $_.NextHop -ne '0.0.0.0'
        } |
        Sort-Object RouteMetric, InterfaceMetric |
        Select-Object -First 1

    if ($route) {
        $ip = Get-NetIPAddress -AddressFamily IPv4 -InterfaceIndex $route.ifIndex -ErrorAction SilentlyContinue |
            Where-Object {
                $_.IPAddress -and
                $_.AddressState -eq 'Preferred' -and
                -not $_.SkipAsSource -and
                $_.IPAddress -notmatch '^(127\.|169\.254\.)'
            } |
            Select-Object -ExpandProperty IPAddress -First 1

        if ($ip) {
            return $ip
        }
    }

    # Fallback: any preferred IPv4 on an adapter that is actually Up
    $upIfIndexes = @(Get-NetAdapter -ErrorAction SilentlyContinue |
        Where-Object { $_.Status -eq 'Up' } |
        Select-Object -ExpandProperty ifIndex)

    if ($upIfIndexes.Count -gt 0) {
        $ip = Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
            Where-Object {
                $_.InterfaceIndex -in $upIfIndexes -and
                $_.IPAddress -and
                $_.AddressState -eq 'Preferred' -and
                -not $_.SkipAsSource -and
                $_.IPAddress -notmatch '^(127\.|169\.254\.)'
            } |
            Select-Object -ExpandProperty IPAddress -First 1

        if ($ip) {
            return $ip
        }
    }

    return $null
}


function Get-TrackedGeo {
    try {
        Write-Log 'Requesting geolocation from ip-api.com'
        return Invoke-RestMethod -Uri 'http://ip-api.com/json/' -TimeoutSec 10 -ErrorAction Stop
    } catch {
        Write-Log "Geolocation lookup failed: $($_.Exception.Message)"
        return $null
    }
}

try {
    Initialize-Log

    try {
        (Get-Process -Id $PID).PriorityClass = 'Idle'
        Write-Log 'Set process priority to Idle'
    } catch {
        Write-Log "Could not set process priority: $($_.Exception.Message)"
    }

    Write-Log "Using API endpoint: $API_URL"

    $ip = $null
    for ($attempt = 1; $attempt -le $MAX_IP_ATTEMPTS; $attempt++) {
        Write-Log "IP capture attempt $attempt of $MAX_IP_ATTEMPTS"
        $ip = Get-TrackedIPv4

        if (-not [string]::IsNullOrWhiteSpace($ip)) {
            Write-Log "IP capture successful: $ip"
            break
        }

        if ($attempt -lt $MAX_IP_ATTEMPTS) {
            Write-Log "No usable IP yet. Retrying in $IP_RETRY_SECONDS seconds"
            Start-Sleep -Seconds $IP_RETRY_SECONDS
        }
    }

    if ([string]::IsNullOrWhiteSpace($ip)) {
        Write-Log 'IP capture failed after 10 minutes. Exiting with failure'
        exit 1
    }

    $geo = Get-TrackedGeo

    $payload = @{
        device_secret_token = $TOKEN
        ip = $ip
        latitude = if ($geo) { $geo.lat } else { $null }
        longitude = if ($geo) { $geo.lon } else { $null }
        accuracy = 1000
    } | ConvertTo-Json

    Write-Log 'Posting telemetry payload to server'
    Invoke-RestMethod -Uri $API_URL -Method Post -Body $payload -ContentType 'application/json' -TimeoutSec 15 -ErrorAction Stop
    Write-Log 'Telemetry POST succeeded. Exiting with success'
    exit 0
} catch {
    Write-Log "Fatal error: $($_.Exception.Message)"
    exit 1
}
