function Merge-TvsHashtable {
    param([hashtable]$Base,[hashtable]$Overlay)
    $result = @{}
    foreach ($k in $Base.Keys) { $result[$k] = $Base[$k] }
    foreach ($k in $Overlay.Keys) {
        if ($result[$k] -is [hashtable] -and $Overlay[$k] -is [hashtable]) {
            $result[$k] = Merge-TvsHashtable -Base $result[$k] -Overlay $Overlay[$k]
        } else {
            $result[$k] = $Overlay[$k]
        }
    }
    $result
}

function Resolve-TvsPath {
    param([string]$WebScriptsRoot,[string]$PathValue)
    if ([string]::IsNullOrWhiteSpace($PathValue)) { return $PathValue }
    if ([System.IO.Path]::IsPathRooted($PathValue)) { return [System.IO.Path]::GetFullPath($PathValue) }
    return [System.IO.Path]::GetFullPath((Join-Path $WebScriptsRoot $PathValue))
}

function Resolve-TvsPathWithFallback {
    param([string]$WebScriptsRoot,[string]$ConfiguredPath,[object[]]$LegacyCandidates,[string]$ConfigKey)
    $resolved = Resolve-TvsPath -WebScriptsRoot $WebScriptsRoot -PathValue $ConfiguredPath
    if (Test-Path $resolved) { return $resolved }

    foreach ($candidate in ($LegacyCandidates | Where-Object { $_ })) {
        $legacy = Resolve-TvsPath -WebScriptsRoot $WebScriptsRoot -PathValue ([string]$candidate)
        if (Test-Path $legacy) {
            Write-VerboseDebug -Timestamp (Get-Date) -Title 'CONFIG' -Message "Falling back to legacy $ConfigKey path: $legacy" -ForegroundColor Yellow
            return $legacy
        }
    }
    return $resolved
}

function Get-TvsConfig {
    param([Parameter(Mandatory=$true)][string]$WebScriptsRoot)

    $defaultsPath = Join-Path $WebScriptsRoot 'config/defaults.json'
    if (-not (Test-Path $defaultsPath)) { throw "Missing required config file: $defaultsPath" }

    try { $defaults = Get-Content -Raw -Path $defaultsPath | ConvertFrom-Json -AsHashtable }
    catch { throw "Invalid defaults.json: $($_.Exception.Message)" }

    $localPath = Join-Path $WebScriptsRoot 'config/local.json'
    $local = @{}
    if (Test-Path $localPath) {
        try { $local = Get-Content -Raw -Path $localPath | ConvertFrom-Json -AsHashtable }
        catch { Write-VerboseDebug -Timestamp (Get-Date) -Title 'CONFIG' -Message "Ignoring invalid local.json: $($_.Exception.Message)" -ForegroundColor Yellow }
    }

    $cfg = Merge-TvsHashtable -Base $defaults -Overlay $local
    foreach ($key in @('commandJson','wtMissionJson','wtConfigBlk','wtUserMissionsDir')) {
        $legacy = @()
        if ($cfg.ContainsKey('legacy') -and $cfg.legacy.ContainsKey($key)) { $legacy = @($cfg.legacy[$key]) }
        $cfg.paths[$key] = Resolve-TvsPathWithFallback -WebScriptsRoot $WebScriptsRoot -ConfiguredPath $cfg.paths[$key] -LegacyCandidates $legacy -ConfigKey $key
    }
    return $cfg
}
