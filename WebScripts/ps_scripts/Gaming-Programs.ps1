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
#     'AuxPrograms' (array of helper executables to auto-launch when the game starts).
#------------------------------------------------------------------------------------
$Global:GameProfiles = @{
    "Notepad.exe"                      = @{ AuxPrograms = @("C:\Users\ralch\Desktop\C-Fanatec Monitor.lnk") } #; BoostAction = "action-per-process-boost1.json" }
    "DCS.exe"                          = @{ Start = "High Performance"; Stop = "Balanced" }
    "aces.exe"                         = @{ NickName = "War thunder"; Start = "High Performance"; Stop = "Balanced" }
    "FlightSimulator.exe"              = @{ NickName = "M.S.F.S.";Start = "High Performance"; Stop = "Balanced" }
    "Ace7Game.exe"                     = @{ NickName = "Ace Comabt"; Start = "Balanced"; Stop = "Balanced" } #; BoostAction = "action-per-process-boost1.json" }
    "forza_steamworks_release_final.exe" = @{ NickName = "Forza"; Start = "Balanced"; Stop = "Balanced"; AuxPrograms = @("C:\Users\ralch\Desktop\C-Fanatec Monitor.lnk", "C:\Users\ralch\Desktop\Disable-Antivirus.ps1.lnk") }
    "forzamotorsport7.exe"             = @{ NickName = "Forza 7"; Start = "Balanced"; Stop = "Balanced"; AuxPrograms = @("C:\Users\ralch\Desktop\C-Fanatec Monitor.lnk", "C:\Users\ralch\Desktop\Disable-Antivirus.ps1.lnk") }
	"ForzaHorizon5.exe"                = @{ NickName = "Forza Horizon"; Start = "Balanced"; Stop = "Balanced"; AuxPrograms = @("C:\Users\ralch\Desktop\C-Fanatec Monitor.lnk", "C:\Users\ralch\Desktop\Disable-Antivirus.ps1.lnk") }
    "AssettoCorsa.exe"                 = @{ NickName = "Asseto Corsa"; Start = "Balanced"; Stop = "Balanced" ; AuxPrograms = @("C:\Users\ralch\Desktop\C-Fanatec Monitor.lnk", "C:\Users\ralch\Desktop\Disable-Antivirus.ps1.lnk") }
	"GRB.exe"                          = @{ NickName = "Ghost Recon Breakpoint"; Start = "Balanced"; Stop = "Balanced" ; AuxPrograms = @("C:\Users\ralch\Desktop\Disable-Antivirus.ps1.lnk", "C:\Users\ralch\Desktop\Spartan Mod.lnk") }
    "TiWorker.exe"                     = @{ Speak = "Windows Ti-Worker process" }
	"CompatTelRunner.exe"              = @{ Speak = "Windows Compat-Tel-Runner process" }
}
# "Ace7Game.exe"                     = @{ Start = "High Performance"; Stop = "Balanced" ; BoostAction = "action-per-process-boost1.json" }
# "Ace7Game.exe"                     = @{ Start = "Balanced"; Stop = "Balanced" } #; BoostAction = "action-per-process-boost1.json" }

#------------------------------------------------------------------------------------
# --- FUNCTIONS ---
# These functions read from the global configuration above. Do not edit these.
#------------------------------------------------------------------------------------

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

