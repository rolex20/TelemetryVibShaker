
# PowerShell 5.1 compatible: PSCustomObject -> Hashtable (recursive)
function ConvertTo-Hashtable {
    param([Parameter(ValueFromPipeline=$true)]$InputObject)

    if ($null -eq $InputObject) { return $null }

    # IDictionary (already a map)
    if ($InputObject -is [System.Collections.IDictionary]) {
        $ht = @{}
        foreach ($k in $InputObject.Keys) {
            $ht[$k] = ConvertTo-Hashtable $InputObject[$k]
        }
        return $ht
    }

    # PSCustomObject (JSON objects land here in PS 5.1)
    if ($InputObject -is [pscustomobject]) {
        $ht = @{}
        foreach ($p in $InputObject.PSObject.Properties) {
            $ht[$p.Name] = ConvertTo-Hashtable $p.Value
        }
        return $ht
    }

    # Enumerable (arrays/lists), but not strings
    if (($InputObject -is [System.Collections.IEnumerable]) -and -not ($InputObject -is [string])) {
        $arr = @()
        foreach ($item in $InputObject) {
            $arr += ,(ConvertTo-Hashtable $item)
        }
        return $arr
    }

    # Primitive
    return $InputObject
}

function Merge-Hashtable {
    param([hashtable]$Base, [hashtable]$Override)

    $out = @{}
    foreach ($k in $Base.Keys) { $out[$k] = $Base[$k] }

    foreach ($k in $Override.Keys) {
        if ($out[$k] -is [hashtable] -and $Override[$k] -is [hashtable]) {
            $out[$k] = Merge-Hashtable $out[$k] $Override[$k]
        } else {
            $out[$k] = $Override[$k]
        }
    }
    return $out
}

# Script-scope state survives across function calls while this script remains loaded.
# We intentionally keep cache metadata here so repeated callers (watchdog, startup, etc.)
# do not re-read/parse JSON unless the file timestamp changed.
if (-not (Get-Variable -Name ConfigPath -Scope Script -ErrorAction SilentlyContinue)) {
    $script:ConfigPath = $null
}
if (-not (Get-Variable -Name CachedConfig -Scope Script -ErrorAction SilentlyContinue)) {
    $script:CachedConfig = $null
}
if (-not (Get-Variable -Name CachedLastWriteTimeUtc -Scope Script -ErrorAction SilentlyContinue)) {
    $script:CachedLastWriteTimeUtc = $null
}

function Get-WebScriptsConfigPath {
    # Resolve once, reuse many times: this avoids rebuilding the path string every poll
    # and keeps all callers aligned to one canonical config location.
    if (-not $script:ConfigPath) {
        $script:ConfigPath = Join-Path $PSScriptRoot "config\hosts.config.json"
    }

    return $script:ConfigPath
}

function Read-WebScriptsConfigRawWithRetry {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConfigPath,
        [int]$MaxAttempts = 5,
        [int]$RetryDelayMilliseconds = 200
    )

    $lastError = $null

    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        try {
            # Some editors save in phases (truncate/write/rename). During that short window
            # we may read malformed/partial content and ConvertFrom-Json will fail.
            # Retry avoids crashing on transient "file in flux" states.
            $rawText = Get-Content -LiteralPath $ConfigPath -Raw -ErrorAction Stop
            $rawObj = $rawText | ConvertFrom-Json -ErrorAction Stop
            $rawHashtable = ConvertTo-Hashtable $rawObj
            return $rawHashtable
        }
        catch {
            $lastError = $_
            if ($attempt -lt $MaxAttempts) {
                # Back off briefly, then re-read a stable version.
                Start-Sleep -Milliseconds $RetryDelayMilliseconds
                continue
            }
        }
    }

    throw "Failed to load '$ConfigPath' after $MaxAttempts attempts. Last error: $($lastError.Exception.Message)"
}

function New-WebScriptsConfigFromFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConfigPath
    )

    $webScriptsRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
    $raw = Read-WebScriptsConfigRawWithRetry -ConfigPath $ConfigPath

    $defaults = @{}
    if ($raw.ContainsKey("defaults") -and ($raw["defaults"] -is [hashtable])) {
        $defaults = $raw["defaults"]
    }

    $machines = @{}
    if ($raw.ContainsKey("machines") -and ($raw["machines"] -is [hashtable])) {
        $machines = $raw["machines"]
    }

    $myhostname = $env:COMPUTERNAME.ToUpperInvariant()

    $hostCfg = @{}
    if ($machines.ContainsKey($myhostname) -and ($machines[$myhostname] -is [hashtable])) {
        $hostCfg = $machines[$myhostname]
    }

    # Merge defaults first, then host overrides. This keeps one baseline schema
    # and lets each machine override only the keys it needs.
    $final = Merge-Hashtable $defaults $hostCfg
    $final["webScriptsRoot"] = $webScriptsRoot.Path
    $final["host"] = $myhostname

    return $final
}

function Get-ConfigSectionAsHashtable {
    param(
        [hashtable]$Config,
        [string]$Key
    )

    # Callers use this helper before diffing. Returning an empty hashtable (instead of $null)
    # keeps diff logic simple and prevents null-guard noise everywhere else.
    if ($Config -and $Config.ContainsKey($Key) -and ($Config[$Key] -is [System.Collections.IDictionary])) {
        return (ConvertTo-Hashtable $Config[$Key])
    }

    return @{}
}

function Test-DeepEqual {
    param(
        $Left,
        $Right
    )

    if ($null -eq $Left -and $null -eq $Right) { return $true }
    if ($null -eq $Left -or $null -eq $Right) { return $false }

    if (($Left -is [System.Collections.IDictionary]) -and ($Right -is [System.Collections.IDictionary])) {
        $leftMap = ConvertTo-Hashtable $Left
        $rightMap = ConvertTo-Hashtable $Right

        $leftKeys = @($leftMap.Keys | ForEach-Object { [string]$_ } | Sort-Object -Unique)
        $rightKeys = @($rightMap.Keys | ForEach-Object { [string]$_ } | Sort-Object -Unique)

        if ($leftKeys.Count -ne $rightKeys.Count) { return $false }

        foreach ($key in $leftKeys) {
            if (-not $rightMap.ContainsKey($key)) { return $false }
            if (-not (Test-DeepEqual -Left $leftMap[$key] -Right $rightMap[$key])) { return $false }
        }

        return $true
    }

    if (($Left -is [System.Collections.IEnumerable]) -and
        ($Right -is [System.Collections.IEnumerable]) -and
        -not ($Left -is [string]) -and
        -not ($Right -is [string])) {
        $leftArray = @($Left)
        $rightArray = @($Right)

        if ($leftArray.Count -ne $rightArray.Count) { return $false }

        # Arrays remain order-sensitive by design. If order changes, we treat it as changed.
        for ($i = 0; $i -lt $leftArray.Count; $i++) {
            if (-not (Test-DeepEqual -Left $leftArray[$i] -Right $rightArray[$i])) {
                return $false
            }
        }

        return $true
    }

    return [object]::Equals($Left, $Right)
}

function Get-RestartReasonsForConfigChange {
    param(
        [hashtable]$OldConfig,
        [hashtable]$NewConfig
    )

    $reasons = @()

    # features.* controls which watchers/jobs are wired at startup.
    # Runtime toggles would leave stale subscriptions alive, so these changes require restart.
    $oldFeatures = Get-ConfigSectionAsHashtable -Config $OldConfig -Key "features"
    $newFeatures = Get-ConfigSectionAsHashtable -Config $NewConfig -Key "features"
    $featureKeys = @($oldFeatures.Keys + $newFeatures.Keys | ForEach-Object { [string]$_ } | Sort-Object -Unique)
    foreach ($featureKey in $featureKeys) {
        $oldHas = $oldFeatures.ContainsKey($featureKey)
        $newHas = $newFeatures.ContainsKey($featureKey)

        if ($oldHas -and $newHas) {
            if (-not (Test-DeepEqual -Left $oldFeatures[$featureKey] -Right $newFeatures[$featureKey])) {
                $reasons += "features.$featureKey changed"
            }
        }
        elseif ($newHas) {
            $reasons += "features.$featureKey added"
        }
        else {
            $reasons += "features.$featureKey removed"
        }
    }

    # Only keys matter for restart: adding/removing programs changes the watcher "wiring" set.
    # Value-only edits (NickName, Speak, BoostAction, etc.) are safe to apply live.
    $oldGameProfiles = Get-ConfigSectionAsHashtable -Config $OldConfig -Key "gameProfiles"
    $newGameProfiles = Get-ConfigSectionAsHashtable -Config $NewConfig -Key "gameProfiles"
    $oldGameProfileKeys = @($oldGameProfiles.Keys | ForEach-Object { [string]$_ } | Sort-Object -Unique)
    $newGameProfileKeys = @($newGameProfiles.Keys | ForEach-Object { [string]$_ } | Sort-Object -Unique)

    $addedGameProfiles = @($newGameProfileKeys | Where-Object { $oldGameProfileKeys -notcontains $_ })
    $removedGameProfiles = @($oldGameProfileKeys | Where-Object { $newGameProfileKeys -notcontains $_ })

    if ($addedGameProfiles.Count -gt 0) {
        $reasons += ("gameProfiles keys added: " + ($addedGameProfiles -join ", "))
    }
    if ($removedGameProfiles.Count -gt 0) {
        $reasons += ("gameProfiles keys removed: " + ($removedGameProfiles -join ", "))
    }

    # Paths can affect where watchers point and where helper files are resolved.
    # Even if some path values are not fully consumed yet, mark restart-needed now to avoid
    # mixed old/new runtime state and to keep future path wiring predictable.
    $oldPaths = Get-ConfigSectionAsHashtable -Config $OldConfig -Key "paths"
    $newPaths = Get-ConfigSectionAsHashtable -Config $NewConfig -Key "paths"
    $pathKeys = @($oldPaths.Keys + $newPaths.Keys | ForEach-Object { [string]$_ } | Sort-Object -Unique)
    foreach ($pathKey in $pathKeys) {
        $oldHas = $oldPaths.ContainsKey($pathKey)
        $newHas = $newPaths.ContainsKey($pathKey)

        if ($oldHas -and $newHas) {
            if (-not (Test-DeepEqual -Left $oldPaths[$pathKey] -Right $newPaths[$pathKey])) {
                $reasons += "paths.$pathKey changed"
            }
        }
        elseif ($newHas) {
            $reasons += "paths.$pathKey added"
        }
        else {
            $reasons += "paths.$pathKey removed"
        }
    }

    return @($reasons)
}

function Get-WebScriptsConfig {
    $cfgPath = Get-WebScriptsConfigPath
    $cfgItem = Get-Item -LiteralPath $cfgPath -ErrorAction Stop
    $currentLastWriteTimeUtc = $cfgItem.LastWriteTimeUtc

    # Fast path: if LastWriteTimeUtc is identical, assume content is unchanged and return cache.
    # UTC avoids local-time ambiguity (DST/locale differences).
    if (($null -ne $script:CachedConfig) -and
        ($null -ne $script:CachedLastWriteTimeUtc) -and
        ($script:CachedLastWriteTimeUtc -eq $currentLastWriteTimeUtc)) {
        return $script:CachedConfig
    }

    $loadedConfig = New-WebScriptsConfigFromFile -ConfigPath $cfgPath
    $script:CachedConfig = $loadedConfig
    $script:CachedLastWriteTimeUtc = $currentLastWriteTimeUtc

    return $loadedConfig
}

function Refresh-WebScriptsConfigIfChanged {
    $result = @{
        Changed = $false
        RestartNeeded = $false
        RestartWillOccur = $false
        Reasons = @()
        Config = $Global:WebScriptsConfig
    }

    $cfgPath = Get-WebScriptsConfigPath
    $cfgItem = Get-Item -LiteralPath $cfgPath -ErrorAction Stop
    $currentLastWriteTimeUtc = $cfgItem.LastWriteTimeUtc

    # First-call bootstrap path (or cache lost): load once, but report Changed=$false.
    # This avoids spurious "config changed" logs at startup.
    if (($null -eq $script:CachedConfig) -or ($null -eq $script:CachedLastWriteTimeUtc)) {
        $Global:WebScriptsConfig = Get-WebScriptsConfig
        if ($Global:WebScriptsConfig.ContainsKey("gameProfiles") -and $Global:WebScriptsConfig["gameProfiles"]) {
            $Global:GameProfiles = $Global:WebScriptsConfig["gameProfiles"]
        }

        $result["Config"] = $Global:WebScriptsConfig
        return $result
    }

    if ($script:CachedLastWriteTimeUtc -eq $currentLastWriteTimeUtc) {
        $result["Config"] = $Global:WebScriptsConfig
        return $result
    }

    $oldConfig = $script:CachedConfig
    $newConfig = $null

    try {
        $newConfig = New-WebScriptsConfigFromFile -ConfigPath $cfgPath
    }
    catch {
        # Soft-fail: keep the last known-good config so watchers continue running.
        # This is important when a save is mid-flight or temporarily malformed.
        Write-VerboseDebug -Timestamp (Get-Date) -Title "CONFIG" -Message "Configuration reload failed after retries: $($_.Exception.Message)" -ForegroundColor "DarkYellow" -Speak $false
        $result["Config"] = $Global:WebScriptsConfig
        return $result
    }

    $script:CachedConfig = $newConfig
    $script:CachedLastWriteTimeUtc = $currentLastWriteTimeUtc
    $Global:WebScriptsConfig = $newConfig

    # Keep runtime profile lookup in sync with refreshed config so process events see
    # the latest value-only edits immediately (no restart required for those).
    if ($Global:WebScriptsConfig.ContainsKey("gameProfiles") -and $Global:WebScriptsConfig["gameProfiles"]) {
        $Global:GameProfiles = $Global:WebScriptsConfig["gameProfiles"]
    }

    $reasons = Get-RestartReasonsForConfigChange -OldConfig $oldConfig -NewConfig $newConfig
    $restartNeeded = ($reasons.Count -gt 0)

    # Safe default is "auto-restart on restart-required changes". Hosts can opt out.
    $restartOnConfigChange = $true
    if ($newConfig.ContainsKey("restart_on_config_change") -and ($null -ne $newConfig["restart_on_config_change"])) {
        $restartOnConfigChange = [bool]$newConfig["restart_on_config_change"]
    }
    $restartWillOccur = ($restartNeeded -and $restartOnConfigChange)

    if (-not $restartNeeded) {
        Write-VerboseDebug -Timestamp (Get-Date) -Title "CONFIG" -Message "Configuration Reloaded" -ForegroundColor "DarkGray" -Speak $false
    }
    else {
        $reasonsText = $reasons -join "; "
        if ($restartWillOccur) {
            $message = "Configuration changed; restart required. Reasons: $reasonsText; restart scheduled."
        }
        else {
            # Speak=true here is intentional: operator still needs to act manually.
            $message = "Configuration changed; restart required. Reasons: $reasonsText; restart recommended (restart_on_config_change=false)."
        }

        Write-VerboseDebug -Timestamp (Get-Date) -Title "CONFIG" -Message $message -ForegroundColor "Yellow" -Speak $true
    }

    $result["Changed"] = $true
    $result["RestartNeeded"] = $restartNeeded
    $result["RestartWillOccur"] = $restartWillOccur
    $result["Reasons"] = @($reasons)
    $result["Config"] = $Global:WebScriptsConfig

    return $result
}

function Bootstrap-Config {
	$Global:WebScriptsConfig = Get-WebScriptsConfig
    if ($Global:WebScriptsConfig.ContainsKey("gameProfiles") -and $Global:WebScriptsConfig["gameProfiles"]) {
        $Global:GameProfiles = $Global:WebScriptsConfig["gameProfiles"]
    }
	return $Global:WebScriptsConfig
}


