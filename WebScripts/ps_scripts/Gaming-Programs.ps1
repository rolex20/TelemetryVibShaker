# Change the functions below to monitor other programs

function Get-GamingPrograms {

    # Exact file name of the games to watch
    $programs = @('DCS.exe', 'aces.exe', 'FlightSimulator.exe') # ,'Notepad.exe', 'cmd.exe', 'mspaint.exe', 

    return $programs

}

function Get-StartPowerSchemes {
    # Use exact file names of the programs
    $powerSchemes = @{
        "DCS.exe" = "High Performance"
        "aces.exe" = "High Performance"
        "FlightSimulator.exe" = "High Performance"
#        "Notepad.exe" = "High Performance"
#        "cmd.exe" = "Balanced with Max 80"
    }

    return $powerSchemes
}

function Get-StopPowerSchemes {
    # Use exact file names of the programs
    $powerSchemes = @{
        "DCS.exe" = "Balanced with Max 80"
        "aces.exe" = "Balanced with Max 80"
        "FlightSimulator.exe" = "Balanced with Max 80"
#        "Notepad.exe" = "Balanced with Max 80"
#        "cmd.exe" = "Balanced with Max 80"
    }

    return $powerSchemes
}
