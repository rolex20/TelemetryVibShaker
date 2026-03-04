<#
.SYNOPSIS
    Provides centralized configuration and functions to manage power schemes and boost actions based on running applications.

.DESCRIPTION
    This script defines a global configuration of applications, their desired power schemes, and optional performance boost profiles. 
    It offers functions to retrieve lists of monitored applications and mappings for their power schemes and boost actions.
    This centralized approach ensures that adding or modifying an application's settings only requires a single change in the configuration variable.

.USAGE
    To add, remove, or change a monitored program, simply edit the $Global:GameProfiles hash table.
    
    Example - Adding a new game with a boost profile:
    Add the following line inside the $Global:GameProfiles definition:
    "NewGame.exe" = @{ Start = "High Performance"; Stop = "Balanced"; BoostAction = "action-per-process-boost-new.json" }
    
    The functions will automatically include the new game without any further modification.
#>

#------------------------------------------------------------------------------------
# --- GLOBAL CONFIGURATION ---
# All programs to be monitored are defined here. This is the only section
# you need to edit to add, remove, or change program settings.
# The structure is a hash table where:
#   - The 'Key' is the process name including the .exe extension (e.g., "DCS.exe").
#   - The 'Value' is another hash table containing the 'Start' and 'Stop' power schemes,
#     and optional fields like 'BoostAction' (JSON file), 'Speak' (text-to-speech), and
#     'AuxPrograms' (array of helper executables to auto-launch when the game starts),
#     optional 'AuxProgramsDelaySeconds' (integer >= 0, fallback 5 when missing/invalid),
#     and optional 'WindowStyle' (Normal/Hidden/Minimized/Maximized for AuxPrograms launch).
#------------------------------------------------------------------------------------

# Prefer GameProfiles from host config (loaded by Start-CommandWatchers via Bootstrap-Config)
if ($Global:WebScriptsConfig -and
    ($Global:WebScriptsConfig -is [hashtable]) -and
    $Global:WebScriptsConfig.ContainsKey('gameProfiles') -and
    $Global:WebScriptsConfig.gameProfiles -and
    $Global:WebScriptsConfig.gameProfiles.Count -gt 0) {
    $Global:GameProfiles = $Global:WebScriptsConfig.gameProfiles
}
else {
    # Fallback (keep tiny, just enough to not break standalone calls)
    $Global:GameProfiles = @{
        "Notepad.exe" = @{ NickName = "Notepad"; ImmediateKill = $false }
    }
}
# "Ace7Game.exe"                     = @{ Start = "High Performance"; Stop = "Balanced" ; BoostAction = "action-per-process-boost1.json" }
# "Ace7Game.exe"                     = @{ Start = "Balanced"; Stop = "Balanced" } #; BoostAction = "action-per-process-boost1.json" }

#------------------------------------------------------------------------------------
# --- FUNCTIONS ---
# These functions read from the global configuration above. Do not edit these.
#------------------------------------------------------------------------------------

function Get-ImmediateKill {
    <#
    .SYNOPSIS
        Returns $true if ImmediateKill is requested for this program
    #>
    param (
        [Parameter(Mandatory)]
        [string]$programName
    )
    
    # Check if a profile exists for the program and if it has a ImmediateKill key.
    if ($Global:GameProfiles.ContainsKey($programName) -and $Global:GameProfiles[$programName].ContainsKey('ImmediateKill')) {
        return $Global:GameProfiles[$programName].ImmediateKill
    }
    
    # Return false if no ImmediateKill value is configured for the program.
    return $false
}

function Get-GamingPrograms {
    <#
    .SYNOPSIS
        Retrieves a list of all configured application process names.
    #>
    return $Global:GameProfiles.Keys
}

function Get-StartPowerSchemes {
    <#
    .SYNOPSIS
        Creates a hash table mapping each application to its designated "Start" power scheme.
    #>
    $schemes = @{}
    foreach ($game in $Global:GameProfiles.GetEnumerator()) {
        $schemes[$game.Name] = $game.Value.Start
    }
    return $schemes
}

function Get-StopPowerSchemes {
    <#
    .SYNOPSIS
        Creates a hash table mapping each application to its designated "Stop" power scheme.
    #>
    $schemes = @{}
    foreach ($game in $Global:GameProfiles.GetEnumerator()) {
        $schemes[$game.Name] = $game.Value.Stop
    }
    return $schemes
}

function Get-GameBoostActions {
    <#
    .SYNOPSIS
        Retrieves the full boost action JSON file path for a given program, if configured.
    #>
    param (
        [Parameter(Mandatory)]
        [string]$programName
    )
    
    # Check if a profile exists for the program and if it has a BoostAction key.
    if ($Global:GameProfiles.ContainsKey($programName) -and $Global:GameProfiles[$programName].ContainsKey('BoostAction')) {
        # Use the automatic $PSScriptRoot variable to build a reliable path to the JSON file.
        # This assumes the boost JSON files are in the same directory as this script.
        return Join-Path $PSScriptRoot $Global:GameProfiles[$programName].BoostAction
    }
    
    # Return null if no boost action is configured for the program.
    return $null
}

function Get-GameSpeakMessage {
    <#
    .SYNOPSIS
        Retrieves the text-to-speech message for a given program, if configured.
    #>
    param (
        [Parameter(Mandatory)]
        [string]$programName
    )
    
    # Check if a profile exists for the program and if it has a Speak key.
    if ($Global:GameProfiles.ContainsKey($programName) -and $Global:GameProfiles[$programName].ContainsKey('Speak')) {
        return $Global:GameProfiles[$programName].Speak
    }
    
    # Return null if no speak action is configured for the program.
    return $null
}

function Get-Stutter {
    <#
    .SYNOPSIS
        Returns $true if Stutter is requested for this program
    #>
    param (
        [Parameter(Mandatory)]
        [string]$programName
    )
    
    # Check if a profile exists for the program and if it has a Stutter key.
    if ($Global:GameProfiles.ContainsKey($programName) -and $Global:GameProfiles[$programName].ContainsKey('Stutter')) {
        return $Global:GameProfiles[$programName].Stutter
    }
    
    # Return null if no Stutter value is configured for the program.
    return $null
}

function Get-GameTtsDisplayName {
    <#
    .SYNOPSIS
        Retrieves the TTS-friendly display name for a given program.
    #>
    param (
        [Parameter(Mandatory)]
        [string]$programName
    )

    if ($Global:GameProfiles.ContainsKey($programName) -and $Global:GameProfiles[$programName].ContainsKey('NickName')) {
        return $Global:GameProfiles[$programName].NickName
    }

    return $programName
}

function Get-GameAuxPrograms {
    <#
    .SYNOPSIS
        Retrieves auxiliary programs to launch when a given program starts, if configured.
    #>
    param (
        [Parameter(Mandatory)]
        [string]$programName
    )

    if ($Global:GameProfiles.ContainsKey($programName) -and $Global:GameProfiles[$programName].ContainsKey('AuxPrograms')) {
        $auxPrograms = $Global:GameProfiles[$programName].AuxPrograms
        if ($null -eq $auxPrograms) {
            return @()
        }

        return $auxPrograms
    }

    return @()
}

function Get-GameAuxProgramsWindowStyle {
    <#
    .SYNOPSIS
        Retrieves per-game window style to use when launching AuxPrograms.
        Returns Minimized when missing/empty.
    #>
    param (
        [Parameter(Mandatory)]
        [string]$programName
    )

    # Backward compatibility baseline:
    # Aux programs have historically launched minimized. Keeping that default means
    # existing profiles do not change behavior just because this optional key exists now.
    $defaultWindowStyle = 'Minimized'

    # We intentionally avoid throwing from config getters because this code runs in
    # event-driven watcher paths. A resilient fallback is safer than breaking an event
    # callback due to one malformed profile entry.
    if (-not $Global:GameProfiles.ContainsKey($programName)) {
        return $defaultWindowStyle
    }

    $profile = $Global:GameProfiles[$programName]
    if (-not ($profile -is [System.Collections.IDictionary])) {
        return $defaultWindowStyle
    }

    if (-not $profile.ContainsKey('WindowStyle')) {
        return $defaultWindowStyle
    }

    $rawWindowStyle = [string]$profile.WindowStyle
    if ([string]::IsNullOrWhiteSpace($rawWindowStyle)) {
        return $defaultWindowStyle
    }

    return $rawWindowStyle.Trim()
}

function Get-GameAuxProgramsDelaySeconds {
    <#
    .SYNOPSIS
        Retrieves per-game aux launch delay in seconds.
        Returns 5 when missing or invalid.
    #>
    param (
        [Parameter(Mandatory)]
        [string]$programName
    )

    # Backward-compat baseline:
    # Old profiles had an implicit ~5s wait before aux launch (because second phase slept 5s).
    # We keep 5 as the default so existing configs do not suddenly change behavior.
    $defaultDelaySeconds = 5

    # Unknown game profile: do not throw inside watcher event handlers.
    # Falling back keeps the pipeline resilient even if config is mid-edit/reload.
    if (-not $Global:GameProfiles.ContainsKey($programName)) {
        return $defaultDelaySeconds
    }

    $profile = $Global:GameProfiles[$programName]
    # JSON objects should land as dictionaries after config normalization.
    # This guard protects us from malformed/hand-built profile values.
    if (-not ($profile -is [System.Collections.IDictionary])) {
        return $defaultDelaySeconds
    }

    # Optional key by design. Missing key means "use legacy behavior".
    if (-not $profile.ContainsKey('AuxProgramsDelaySeconds')) {
        return $defaultDelaySeconds
    }

    $rawDelay = $profile.AuxProgramsDelaySeconds
    # Null is treated as "not configured" instead of an error.
    if ($null -eq $rawDelay) {
        return $defaultDelaySeconds
    }

    $parsedDelay = 0
    # TryParse prevents exceptions for values like "abc" and keeps watcher flow stable.
    # Casting first to string lets us accept numeric strings from JSON/editor input.
    if (-not [int]::TryParse([string]$rawDelay, [ref]$parsedDelay)) {
        return $defaultDelaySeconds
    }

    # Negative delays are nonsensical for scheduling. Clamp via fallback.
    if ($parsedDelay -lt 0) {
        return $defaultDelaySeconds
    }

    # At this point we have a valid, non-negative integer delay in seconds.
    return $parsedDelay
}
