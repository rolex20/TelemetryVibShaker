. "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts\Include-Script.ps1"

$search_paths = @("C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts", "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts")

$include_file = Include-Script -FileName "Write-VerboseDebug.ps1" -Directories $search_paths
. $include_file

$include_file = Include-Script -FileName "Gaming-Programs.ps1" -Directories $search_paths
. $include_file

$include_file = Include-Script -FileName "Set-PowerScheme.ps1" -Directories $search_paths
. $include_file



function Set-GamePowerScheme($traceName, $programName) {


    $newPowerScheme = "Invalid trace: $traceName"
    $powerSchemes = $null


    switch ($traceName) 
    {
        "Win32_ProcessStartTrace" { $powerSchemes = Get-StartPowerSchemes }
        "Win32_ProcessStopTrace" { $powerSchemes = Get-StopPowerSchemes }
    }

    if ($powerSchemes) {
        Add-Type -AssemblyName PresentationCore
        $player = New-Object System.Windows.Media.MediaPlayer
        $player.Open([uri] "N:\MyPrograms\MySounds\Thirdwire\fasten_seatbelt.wav")
        $player.Play()

        $newPowerScheme =  $powerSchemes[$programName]

        if ($newPowerScheme) 
        {
            Set-PowerScheme -schemeName $newPowerScheme -delay 5
        } else {
            Write-VerboseDebug -Timestamp (Get-Date) -Title " " -ForegroundColor "Red" -Speak $true -Message "Power scheme not found for program $programName"
        }

    } else {
        Write-VerboseDebug -Timestamp (Get-Date) -Title " " -ForegroundColor "Red" -Speak $true -Message "Invalid trace name: $traceName"
    }


}

