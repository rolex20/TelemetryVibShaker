# Change power scheme

function Get-ActivePowerPlanName {
    $activeScheme = powercfg /GETACTIVESCHEME
    if ($activeScheme -match 'Power Scheme GUID: (.+) \((.+)\)') {
        return $matches[2]
    }
}


# Function to get the current power scheme (GUID)
function Get-CurrentPowerScheme {
    $scheme = powercfg /GETACTIVESCHEME
    if ($scheme -match 'GUID: ([\w-]+)') {
        return $matches[1]
    }
    return $null
}

# Function to set the power scheme
function Set-PowerScheme {
    param (
        [string]$schemeName,
        [int]$delay = 0
    )



    # Internal list of GUIDs for power schemes
    $powerSchemes = @{
        "High Performance" = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"
        "Balanced" = "381b4222-f694-41f0-9685-ff5bb260df2e"
        "Balanced with Max 80"= "8ce70147-6e1b-4074-8cf4-74cd44daa9e9"
    }

    if ($powerSchemes.ContainsKey($schemeName)) {
        $schemeGuid = $powerSchemes[$schemeName]
        $currentScheme = Get-CurrentPowerScheme
        if ($currentScheme -ne $schemeGuid) {
            if ($delay -gt 0) {Start-Sleep -Seconds $delay }
            powercfg /SETACTIVE $schemeGuid
            $msg = "Power scheme changed to $schemeName." 
            Write-VerboseDebug -Timestamp (Get-Date) -Title "P-TRACE" -Message $msg -ForegroundColor "White" -Speak $true
        } else {
            $msg = "Power scheme is already set to $schemeName."
            Write-VerboseDebug -Timestamp (Get-Date) -Title "P-TRACE" -Message $msg -ForegroundColor "Yellow" -Speak $true
        }
    } else {
        Write-VerboseDebug -Timestamp (Get-Date) -Title " " -ForegroundColor "Red" -Speak $true -Message "Invalid power scheme name: $schemeName. Valid options are 'High Performance' or 'Balanced'."
    }

}

# Usage Examples
#    Set-PowerScheme -schemeName "High Performance"
#    Set-PowerScheme -schemeName "Balanced"
