$PSCommandPath


# This script monitors a specified file for changes. When the file is modified,
# the script downloads a file from a provided URL and saves it to a designated path.
# The script then sleeps for a set duration and repeats the process, waiting
# for the next modification on the watch file.

. "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\ps_scripts\Write-VerboseDebug.ps1"
. "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\ps_scripts\Get-RenamesWatcher.ps1"
. "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\ps_scripts\Include-Script.ps1"
. "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\ps_scripts\Watchdog-Operations.ps1"

$need_restart = $false
try {

    $EfficiencyAffinity = 983040 # HyperThreading enabled
    try {
        [System.Diagnostics.Process]::GetCurrentProcess().ProcessorAffinity =  $EfficiencyAffinity
    } catch {
        Write-VerboseDebug -Timestamp (Get-Date) -Title "WARNING" -Message "Not an Intel 12700K or 14700K " -ForegroundColor "Yellow"
    }

    $title = 'Wait for JSON Gaming Commands'
    $Host.UI.RawUI.WindowTitle = $title


    # INITIALIZATION: Make sure this is the only instance running.

    # Define the name of the mutex, to prevent other instances
    #$mutexName = "Global\$title"
    $mutexName = "$title"

    # Initialize the variable that will be used to check if the mutex is new
    $isNew = $false

    # Create the Mutex
    $mutex_owner = $false
    $mutex = New-Object System.Threading.Mutex($false, $mutexName, [ref]$isNew)

    if ($isNew) {
        $r = $mutex.WaitOne()
        $mutex_owner = $true
        Write-VerboseDebug -Timestamp (Get-Date) -Title "STARTING" -Message "This is the only instance running" -ForegroundColor "Gray"
    } else {
	    Write-VerboseDebug -Timestamp (Get-Date) -Title "ERROR" -Message "There is another instance running" -ForegroundColor "Red"
	    Start-Sleep -Seconds 5
	    Exit 
    }



    # JSON COMMANDS FOR REMOTE CONTROL
    # command.json is created by wamp/apache/http/php script
    # remote commands are sent in support of my TelemetryVibShaker Apps

    $remote_commands = {
        . "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\ps_scripts\Process-CommandFromJson.ps1"

        $path = $Event.SourceEventArgs.FullPath
        $changeType = $Event.SourceEventArgs.ChangeType

        Write-Host " "
        Write-VerboseDebug -Timestamp $Event.TimeGenerated -Title "CHANGE_TYPE:$changeType" -Message $path

        Process-CommandFromJson $path
    }


    # LISTEN FOR COMMANDS HERE
    $command_file = "C:\wamp\www\remote_control\command.json"
    $watcher_objects = Get-RenamesWatcher $command_file $remote_commands


    #Infinite loop to periodically check-alive
    Watchdog_Operations

} #end-try

finally {
    Write-VerboseDebug -Timestamp (Get-Date) -Title "FINALLY" -Message "Disposing objects" -ForegroundColor "Yellow"
  
    if ($watcher_objects) {
        $watcher_objects[1].EnableRaisingEvents = $false
        $watcher_objects[1].Dispose() #Dispose the FileSystemWatcher
        $watcher_objects[0].Dispose() #Dispose the Handler
    }


    if ($mutex_owner) {
        $mutex.ReleaseMutex()
    }
    $mutex.Close()  
    $mutex.Dispose()


}

if ($need_restart) {
    Start-Sleep -Seconds 2
    Start-Process -FilePath "powershell.exe" -ArgumentList "-File `"$PSCommandPath`""
}