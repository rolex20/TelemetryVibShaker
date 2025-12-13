. "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts\Include-Script.ps1"

$search_paths = @("C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts", "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts")

$include_file = Include-Script -FileName "Write-VerboseDebug.ps1" -Directories $search_paths
. $include_file

$include_file = Include-Script -FileName "Gaming-Programs.ps1" -Directories $search_paths
. $include_file

$include_file = Include-Script -FileName "Set-PowerScheme.ps1" -Directories $search_paths
. $include_file

$include_file = Include-Script -FileName "Set-IdealProcessor.ps1" -Directories $search_paths
. $include_file

function Restore-GameBoost {
    <#
    .SYNOPSIS
        Restores processes affected by a specific game's boost profile back to a default state intelligently.
    #>
    param (
        [Parameter(Mandatory)]
        [string]$programNameWithExt,
        
        [Parameter(Mandatory)]
        [string]$boostJsonPath,
        
        [int]$threadsLimit = 50
    )
    
    # Normalize the incoming program name by removing the .exe extension for JSON lookup.
    $normalizedProgramName = $programNameWithExt -replace '\.exe$', ''
    
    $actions = Get-ActionsPerGame $boostJsonPath

    Write-VerboseDebug -Timestamp (Get-Date) -Title "RESTORE" -Message "Attempting to restore processes affected by '$normalizedProgramName' boost." -ForegroundColor "Yellow"

    # Find the specific action block for the program that just stopped using the NORMALIZED name.
    $gameAction = $actions | Where-Object { $_.process_name -eq $normalizedProgramName } | Select-Object -First 1

    if (-not $gameAction) {
        Write-VerboseDebug -Timestamp (Get-Date) -Title "RESTORE" -Message "No matching boost profile found for '$normalizedProgramName' in '$boostJsonPath'. Nothing to restore." -ForegroundColor "Gray"
        return
    }

    # The main game process is already stopped, so we only need to restore its dependencies that may still be running.
    if ($gameAction.parameters.dependencies) {
        foreach ($dependence in $gameAction.parameters.dependencies) {
            $dep_proc_names_array = Get-TrimmedProcessNames $dependence.process_name
            foreach ($dep_proc_name in $dep_proc_names_array) {
                $depProcesses = Get-Process -Name $dep_proc_name -ErrorAction SilentlyContinue
                foreach($depProcess in $depProcesses) {
                    Write-Host "RESTORING (Dependency): [$($depProcess.Name)]" -ForegroundColor Cyan
                    # Call the new intelligent restore function, passing the dependency's original parameters.
                    Restore-ProcessToDefaults -Process $depProcess -OriginalParameters $dependence -threadsLimit $threadsLimit
                }
            }
        }
    }
}

function Play-SeatBelt() {
        Add-Type -AssemblyName PresentationCore
        $player = New-Object System.Windows.Media.MediaPlayer
        $player.Open([uri] "N:\MyPrograms\MySounds\Thirdwire\fasten_seatbelt.wav")
        $player.Play()	
		Start-Sleep -Seconds 1
}

function Set-GamePowerScheme($traceName, $programName) {
    $powerSchemes = $null

        # Check if there is a Speak action defined
        $speakText = Get-GameSpeakMessage -programName $programName

        # Retrieve any auxiliary programs configured for this title
        $auxPrograms = Get-GameAuxPrograms -programName $programName

        # First, do the power scheme
    switch ($traceName) {
        "Win32_ProcessStartTrace" {
            $powerSchemes = Get-StartPowerSchemes
			if ($speakText) {
				Write-VerboseDebug -Timestamp (Get-Date) -Title "PROCESS STARTED:" -ForegroundColor "Yellow" -Speak $true -Message $speakText
			}
        }
        "Win32_ProcessStopTrace" { 
            $powerSchemes = Get-StopPowerSchemes 
			if ($speakText) {
				Write-VerboseDebug -Timestamp (Get-Date) -Title "PROCESS EXIT:" -ForegroundColor "Yellow" -Speak $true -Message $speakText
			}			
		}
	}
	
    # The power scheme logic remains correct because it uses the original name (with .exe)
    # which matches the keys in the hash table returned by Get-Start/StopPowerSchemes.
    if ($powerSchemes) {		
        $newPowerScheme = $powerSchemes[$programName]
        if ($newPowerScheme) {
            Set-PowerScheme -schemeName $newPowerScheme -delay 3
        } else {
            Write-VerboseDebug -Timestamp (Get-Date) -Title "POWER" -ForegroundColor "Yellow" -Speak $false -Message "Power scheme not found for program '$programName'"
        }
    } else {
        Write-VerboseDebug -Timestamp (Get-Date) -Title "ERROR" -ForegroundColor "Red" -Speak $true -Message "Invalid trace name: '$traceName'"
    }	
            
 			
	# Next, do the boost if required by the game
	Start-Sleep -Seconds 5	
    switch ($traceName) {
        "Win32_ProcessStartTrace" {
            #$powerSchemes = Get-StartPowerSchemes

            # Launch auxiliaries linked to the game
            foreach ($aux in $auxPrograms) {
                if ([string]::IsNullOrWhiteSpace($aux)) {
                    continue
                }

                if (Test-Path $aux) {
                    Write-VerboseDebug -Timestamp (Get-Date) -Title "AUX START" -ForegroundColor "Green" -Message "Launching $aux"
                    Start-Process -FilePath $aux
                }
                else {
                    Write-VerboseDebug -Timestamp (Get-Date) -Title "AUX START" -ForegroundColor "DarkYellow" -Message "Aux program not found: $aux"
                }
            }

            # Look for a boost action using the full program name (with .exe).
            $boostJsonPath = Get-GameBoostActions -programName $programName
            if ($boostJsonPath -and (Test-Path $boostJsonPath)) {
                                Play-SeatBelt
                Write-VerboseDebug -Timestamp (Get-Date) -Title "BOOST" -Message "Applying boost for '$programName' from '$boostJsonPath'" -ForegroundColor "Green"
                # Call Run-Actions-Per-Game with the EXTENSION-LESS name for JSON compatibility.
                $normalizedProgramName = $programName -replace '\.exe$', ''
                Run-Actions-Per-Game -processName $normalizedProgramName -fileName $boostJsonPath -threadsLimit 50
            }
        }
        "Win32_ProcessStopTrace" { 
            #$powerSchemes = Get-StopPowerSchemes 
            
            # Look for a boost action using the full program name (with .exe).
            $boostJsonPath = Get-GameBoostActions -programName $programName
            if ($boostJsonPath -and (Test-Path $boostJsonPath)) {
				Play-SeatBelt
                Write-VerboseDebug -Timestamp (Get-Date) -Title "RESTORE" -Message "Restoring processes related to '$programName' using '$boostJsonPath'" -ForegroundColor "Cyan"
                # Call our corrected restore function.
                Restore-GameBoost -programName $programName -boostJsonPath $boostJsonPath -threadsLimit 50
            }
        }
    }
	

	if ($speakText) {
		Write-VerboseDebug -Timestamp (Get-Date) -Title "LAUNCH DETECTED" -ForegroundColor "Yellow" -Speak $true -Message $speakText

	}


}