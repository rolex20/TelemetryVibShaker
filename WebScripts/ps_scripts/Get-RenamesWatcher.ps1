
function Get-RenamesWatcher($WatchFile, $Action)
{

    $filewatcher = New-Object System.IO.FileSystemWatcher
    $filewatcher.Path = [System.IO.Path]::GetDirectoryName($WatchFile)
    $filewatcher.Filter = [System.IO.Path]::GetFileName($WatchFile)
    $filewatcher.IncludeSubdirectories = $false
    $filewatcher.EnableRaisingEvents = $true

    $event = Register-ObjectEvent $filewatcher "Renamed" -Action $Action
    
    #Return an array with two objects
    return ($event, $filewatcher)
}

# Example utilization

<#
. "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\ps_scripts\Write-VerboseDebug.ps1"

$writeaction1 = {
    . "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\ps_scripts\Write-VerboseDebug.ps1"
    $path = $Event.SourceEventArgs.FullPath
    $changeType = $Event.SourceEventArgs.ChangeType
    $formattedTimestamp = $(Get-Date).ToString("yyyy-MM-dd HH:mm:ss.fffffff")
    $logline = "$formattedTimestamp, $changeType, $path"

    $t = Get-Date
    Write-Host " "
    Write-VerboseDebug -Timestamp $Event.TimeGenerated -Title $changeType -Message $path
}    

$writeaction2 = { 
    . "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\ps_scripts\Write-VerboseDebug.ps1"
    $path = $Event.SourceEventArgs.FullPath
    $changeType = $Event.SourceEventArgs.ChangeType
    $formattedTimestamp = $(Get-Date).ToString("yyyy-MM-dd HH:mm:ss.fffffff")
    $logline = "$formattedTimestamp, $changeType, $path"

    $t = Get-Date
    Write-Host " "
    Write-VerboseDebug -Timestamp $Event.TimeGenerated -Title $changeType -Message $path    
}    


$command1 = Get-RenamesWatcher "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\command.json" $writeaction1
$command2 = Get-RenamesWatcher "C:\wamp\www\warthunder\mission_data.json" $writeaction2


$t = Get-Date
Write-Host " "
Write-VerboseDebug -Timestamp $t -Title "STARTING" -Message "Waiting for changes"
#>