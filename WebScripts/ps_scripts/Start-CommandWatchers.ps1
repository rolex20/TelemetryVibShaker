# This script monitors a specified file for changes. When the file is modified,
# the script downloads a file from a provided URL and saves it to a designated path.
# The script then sleeps for a set duration and repeats the process, waiting
# for the next modification on the watch file.


. ".\Write-VerboseDebug.ps1"
. ".\Get-RenamesWatcher.ps1"
. ".\Watchdog-Operations.ps1"
. ".\SetAffinityAndPriority.ps1"
. ".\Get-ProcessWatcher.ps1"
. ".\Send-IPC-ExitCommand.ps1"
. ".\Check-Admin-Privileges.ps1"

. ".\Get-HostConfig.ps1"
Bootstrap-Config


$need_restart = $false
# Used by EXIT_WATCHER to gracefully unwind the watchdog loop into finally {} cleanup.
$Global:Watcher_Continue = $true
try {
	# 0- SET TITLE
		$title = 'Watcher for My Gaming Commands'
		$Host.UI.RawUI.WindowTitle = $title

    # 1- DECONFLICT: Make sure this is the only instance running.

        # Define the name of the mutex, to prevent other instances
        #$mutexName = "Global\$title"
        $mutexName = "$title"

        # Initialize the variable that will be used to check if the mutex is new
        $isNew = $false

        # Create the Mutex
        $mutex_owner = $false
        $mutex = New-Object System.Threading.Mutex($false, $mutexName, [ref]$isNew)
		$r = $mutex.WaitOne(1000)
        if ($r) {          
            $mutex_owner = $true
            Write-VerboseDebug -Timestamp (Get-Date) -Title "STARTING" -Message "This is the only instance running" -ForegroundColor "Gray"
        } else {
	        Write-VerboseDebug -Timestamp (Get-Date) -Title "ERROR" -Message "There is another instance running" -ForegroundColor "Red"
	        Start-Sleep -Seconds 5
	        Exit 
        }

    # 2- ADJUST PRIORITIES, AFFINITIES, ETC

        Check-Admin-Privileges | Out-Null

        $EfficiencyAffinity = 983040 # HyperThreading enabled
        try {
            [System.Diagnostics.Process]::GetCurrentProcess().ProcessorAffinity =  $EfficiencyAffinity
        } catch {
            Write-VerboseDebug -Timestamp (Get-Date) -Title "WARNING" -Message "Not an Intel 12700K or 14700K " -ForegroundColor "Yellow"
        }
		
		SetAffinityAndPriority -SetEfficiencyAffinity $true -SetBackgroudPriority $false -SetIdlePriority $true



    # 3- JSON COMMANDS FOR REMOTE CONTROL
    # command.json is created by is created by wamp/apache/http/php/command.php script
    # remote commands are sent in support of my TelemetryVibShaker Apps
	
        $remote_commands = {
            . ".\Process-CommandFromJson.ps1"

            $path = $Event.SourceEventArgs.FullPath
            $changeType = $Event.SourceEventArgs.ChangeType

            Write-Host " "
            Write-VerboseDebug -Timestamp $Event.TimeGenerated -Title "CHANGE_TYPE:$changeType" -Message $path

            Process-CommandFromJson $path
        }
		
		

		# create the object in this scope, outside the if {}
		$cm_watcher_objects = Get-Date
		$x = $cfg.features.remoteCommandsWatcher
		
		write-host "[$x]"
		#i put an exit below because $x is comming empty instead of $true
		Exit
		
		if ($cfg.features.remoteCommandsWatcher) {
			# Setup filesystem watch events for json remote control commands
			$command_file = "C:\MyPrograms\wamp\www\remote_control\command.json"
			$cm_watcher_objects = Get-RenamesWatcher $command_file $remote_commands
			write-host "get-renameswatcher called"
		}
		Start-Sleep 3600

		



    # 4- JSON PARAMETERS FOR WAR THUNDER MISSION GENERATOR
    # mission_data.json is created by wamp/apache/http/php/mission1.php script

        $generate_mission1 = {
            . ".\WT_MissionType1.ps1"

            $path = $Event.SourceEventArgs.FullPath
            $changeType = $Event.SourceEventArgs.ChangeType

            Write-Host " "
			Write-VerboseDebug -Timestamp $Event.TimeGenerated -Title "CHANGE_TYPE:$changeType" -Message $path
            #Write-VerboseDebug -Timestamp $Event.TimeGenerated -Title "WAR THUNDER" -Message "Generate Mission"
            Generate_WT_Mission_Type1

        }

        # Setup filesystem watch events for war thunder mission type 1 generation        
        $mission1 = "C:\wamp\www\warthunder\mission_data.json"  #Alienware
		$mission1 = "C:\MyPrograms\wamp\www\warthunder\mission_data.json" #Galvatron
        $wt_watcher_objects = Get-RenamesWatcher $mission1 $generate_mission1


    # 5- POWER SCHEME PARAMETERS FOR PROCESS WATCHER
        $processAction = {
            . ".\Set-GamePowerScheme.ps1"

            $event_pId = $Event.SourceEventArgs.NewEvent.ProcessID.ToString()
            $pName = $Event.SourceEventArgs.NewEvent.ProcessName.ToString()
            $traceName = $Event.SourceEventArgs.NewEvent.ToString()

            Write-Host " "            
            Write-VerboseDebug -Timestamp $Event.TimeGenerated -Title "PROCESS" -Message "$traceName - $pName [$event_pId]"
            Set-GamePowerScheme -traceName $traceName -programName $pName -processId $event_pId
        }
        $processWatcher = Get-ProcessWatchers $processAction        

    # 7- START NEW IPC PIPE SERVER THREAD FOR SPECIAL COMMANDS (SHOW PROCESS TIMES REQUIRES INITIAL STATE MEMORY)
        . ".\Declare-IPC-Server-Action.ps1" -Directories $search_paths

        $job = Start-ThreadJob -ScriptBlock $ipc_job_action -ThrottleLimit 5 -StreamingHost $Host


    # 8 - Report whenever war-thunder rewrites my customized distance multiplier
        . ".\Monitor-War-Thunder-Distance-Multiplier.ps1"
            

    # 9- WATCHDOG: Infinite loop to periodically check-alive in filesystem-watch which some times fails or locks
        $Global:Watcher_Continue = $true
        $need_restart = Watchdog_Operations

} #end try block

finally {
	Set-MpPreference -DisableRealtimeMonitoring $false -Force	
	Set-MpPreference -ScanOnlyIfIdleEnabled $false
		
    Send-IPC-ExitCommand "ipc_pipe_vr_server_commands"	
	
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

	Write-VerboseDebug -Timestamp (Get-Date) -Title "FINALLY" -Message "Removing CimIndicationEvents" -ForegroundColor "Yellow"	
    Get-Event | Remove-Event -ErrorAction SilentlyContinue
    Get-EventSubscriber | Unregister-Event -ErrorAction SilentlyContinue
} #end finally block

if ($need_restart) {
    $delay_sec = 5
    $message = "WARNING: Restarting the Watcher in $delay_sec seconds."
    Write-VerboseDebug -Timestamp (Get-Date) -Title "WARNING.  " -Message $message -ForegroundColor "Red"
    Start-Sleep -Seconds $delay_sec
    Start-Process -FilePath "powershell.exe" -ArgumentList "-File `"$PSCommandPath`""
}
