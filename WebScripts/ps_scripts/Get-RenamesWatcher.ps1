<#
# Function: Get-RenamesWatcher
# Description: Creates a file watcher to monitor a specified file for rename events.
#             When the file is renamed, the provided action delegate is called.
# Parameters:
#   - WatchFile: The path to the file to watch for renames.
#   - Action: The script block to be called when the file is renamed.
#             Receives the renamed file path as a parameter.
# Returns:
#   An array containing the event object and the file watcher object.
#
# Example 1: Monitor a file for renames and log the new name
# 
# $watchFile = "C:\path\to\file.txt"
# $action = {
#   param($renamedFile)
#   Write-Host "File renamed to: $renamedFile"
#   # Add logic to process the renamed file here
# }
# 
# $event, $filewatcher = Get-RenamesWatcher -WatchFile $watchFile -Action $action
# 
# Start the file watcher (wait for events)
# $filewatcher.BeginWatching()
# 
# Stop the file watcher when finished
# $filewatcher.StopWatching()


# Example utilization 2
. "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts\Write-VerboseDebug.ps1"

$writeaction1 = {
    . "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts\Write-VerboseDebug.ps1"
    $path = $Event.SourceEventArgs.FullPath
    $changeType = $Event.SourceEventArgs.ChangeType
    $formattedTimestamp = $(Get-Date).ToString("yyyy-MM-dd HH:mm:ss.fffffff")
    $logline = "$formattedTimestamp, $changeType, $path"

    $t = Get-Date
    Write-Host " "
    Write-VerboseDebug -Timestamp $Event.TimeGenerated -Title $changeType -Message $path
}    

$writeaction2 = { 
    . "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts\Write-VerboseDebug.ps1"
    $path = $Event.SourceEventArgs.FullPath
    $changeType = $Event.SourceEventArgs.ChangeType
    $formattedTimestamp = $(Get-Date).ToString("yyyy-MM-dd HH:mm:ss.fffffff")
    $logline = "$formattedTimestamp, $changeType, $path"

    $t = Get-Date
    Write-Host " "
    Write-VerboseDebug -Timestamp $Event.TimeGenerated -Title $changeType -Message $path    
}    


$command1 = Get-RenamesWatcher "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\command.json" $writeaction1
$command2 = Get-RenamesWatcher "C:\wamp\www\warthunder\mission_data.json" $writeaction2


$t = Get-Date
Write-Host " "
Write-VerboseDebug -Timestamp $t -Title "STARTING" -Message "Waiting for changes"
#>
function Get-RenamesWatcher($WatchFile, $Action)
{

    $filewatcher = New-Object System.IO.FileSystemWatcher
    $filewatcher.Path = [System.IO.Path]::GetDirectoryName($WatchFile)
    $filewatcher.Filter = [System.IO.Path]::GetFileName($WatchFile)
    $filewatcher.IncludeSubdirectories = $false
    $filewatcher.EnableRaisingEvents = $true

    $event = Register-ObjectEvent $filewatcher "Renamed" -Action $Action
    
    # Return the event object and the file watcher object.
    return ($event, $filewatcher)
}

# Function: Get-ModificationWatcher
# Description: Creates a file watcher to monitor a specified file for modification events.
#             When the file is modified, the provided action delegate is called.
# Parameters:
#   - WatchFile: The path to the file to watch for modifications.
#   - Action: The script block to be called when the file is modified.
#             Receives the modified file path as a parameter.
# Returns:
#   An array containing the event object and the file watcher object.
function Get-ModificationWatcher($WatchFile, $Action)
{
    $filewatcher = New-Object System.IO.FileSystemWatcher
    $filewatcher.Path = [System.IO.Path]::GetDirectoryName($WatchFile)
    $filewatcher.Filter = [System.IO.Path]::GetFileName($WatchFile)
    $filewatcher.IncludeSubdirectories = $false
    $filewatcher.EnableRaisingEvents = $true
    $event = Register-ObjectEvent $filewatcher "Changed" -Action $Action

    # Return the event object and the file watcher object.
    return ($event, $filewatcher)
}



