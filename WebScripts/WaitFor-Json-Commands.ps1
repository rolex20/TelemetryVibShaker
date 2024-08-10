﻿# This script monitors a specified file for changes. When the file is modified,
# the script downloads a file from a provided URL and saves it to a designated path.
# The script then sleeps for a set duration and repeats the process, waiting
# for the next modification on the watch file.


. "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WebScripts\ps_scripts\Include-Script.ps1"

<#
. "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WebScripts\ps_scripts\Write-VerboseDebug.ps1"
. "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WebScripts\ps_scripts\Get-RenamesWatcher.ps1"
. "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WebScripts\ps_scripts\Include-Script.ps1"
. "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WebScripts\ps_scripts\Watchdog-Operations.ps1"
#>


# INCLUDE COMMON FUNCTIONS

$search_paths = @("C:\MyPrograms\MyApps\TelemetryVibShaker\WebScripts", "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WebScripts\ps_scripts")

$include_file = Include-Script -FileName "Write-VerboseDebug.ps1" -Directories $search_paths
. $include_file

$include_file = Include-Script -FileName "Get-RenamesWatcher.ps1" -Directories $search_paths
. $include_file

$include_file = Include-Script -FileName "Include-Script.ps1" -Directories $search_paths
. $include_file

$include_file = Include-Script -FileName "Watchdog-Operations.ps1" -Directories $search_paths
. $include_file


$need_restart = $false
try {

    # 1- ADJUST PRIORITIES, AFFINITIES, ETC

        $EfficiencyAffinity = 983040 # HyperThreading enabled
        try {
            [System.Diagnostics.Process]::GetCurrentProcess().ProcessorAffinity =  $EfficiencyAffinity
        } catch {
            Write-VerboseDebug -Timestamp (Get-Date) -Title "WARNING" -Message "Not an Intel 12700K or 14700K " -ForegroundColor "Yellow"
        }

        $title = 'Wait for JSON Gaming Commands'
        $Host.UI.RawUI.WindowTitle = $title




    # 2- DECONFLICT: Make sure this is the only instance running.

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




    # 3- JSON COMMANDS FOR REMOTE CONTROL
    # command.json is created by is created by wamp/apache/http/php/command.php script
    # remote commands are sent in support of my TelemetryVibShaker Apps

        $remote_commands = {
            . "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WebScripts\ps_scripts\Process-CommandFromJson.ps1"

            $path = $Event.SourceEventArgs.FullPath
            $changeType = $Event.SourceEventArgs.ChangeType

            Write-Host " "
            Write-VerboseDebug -Timestamp $Event.TimeGenerated -Title "CHANGE_TYPE:$changeType" -Message $path

            Process-CommandFromJson $path
        }


        # Setup filesystem watch events for json remote control commands
        $command_file = "C:\wamp\www\remote_control\command.json"
        $cm_watcher_objects = Get-RenamesWatcher $command_file $remote_commands




    # 4- JSON PARAMETERS FOR WAR THUNDER MISSION GENERATOR
    # mission_data.json is created by wamp/apache/http/php/mission1.php script

        $generate_mission1 = {
            . "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WebScripts\ps_scripts\WT_MissionType1.ps1"

            $path = $Event.SourceEventArgs.FullPath
            $changeType = $Event.SourceEventArgs.ChangeType

            Write-Host " "
            Write-VerboseDebug -Timestamp $Event.TimeGenerated -Title "WAR THUNDER" -Message "Generate Mission"
            Generate_WT_Mission_Type1

        }

        # Setup filesystem watch events for war thunder mission type 1 generation
        $mission1 = "C:\MyPrograms\wamp\www\warthunder\mission_data.json" #Galvatron
        $mission1 = "C:\wamp\www\warthunder\mission_data.json"  #Alienware
        $wt_watcher_objects = Get-RenamesWatcher $mission1 $generate_mission1



    # 5- WATCHDOG: Infinite loop to periodically check-alive in filesystem-watch which some times fails or locks
        Watchdog_Operations

} #end try block

finally {
    Write-VerboseDebug -Timestamp (Get-Date) -Title "FINALLY" -Message "Disposing objects" -ForegroundColor "Yellow"
  
    if ($cm_watcher_objects) {
        $cm_watcher_objects[1].EnableRaisingEvents = $false
        $cm_watcher_objects[1].Dispose() #Dispose the FileSystemWatcher
        $cm_watcher_objects[0].Dispose() #Dispose the Handler
    }

    if ($wt_watcher_objects) {
        $wt_watcher_objects[1].EnableRaisingEvents = $false
        $wt_watcher_objects[1].Dispose() #Dispose the FileSystemWatcher
        $wt_watcher_objects[0].Dispose() #Dispose the Handler
    }

    if ($mutex_owner) {
        $mutex.ReleaseMutex()
    }
    $mutex.Close()  
    $mutex.Dispose()


} #end finally block

if ($need_restart) {
    Start-Sleep -Seconds 2
    Start-Process -FilePath "powershell.exe" -ArgumentList "-File `"$PSCommandPath`""
}