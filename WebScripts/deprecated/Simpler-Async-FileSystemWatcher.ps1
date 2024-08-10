﻿# https://dotnet-helpers.com/powershell/how-to-monitor-a-folder-changes-using-powershell/

# Este fue el template final que use para Get-RenamesWatcher

### SET FOLDER TO WATCH + FILES TO WATCH + SUBFOLDERS YES/NO
    $filewatcher = New-Object System.IO.FileSystemWatcher
    #Mention the folder to monitor
    $filewatcher.Path = "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator"
    $filewatcher.Filter = "command.json"
    #include subdirectories $true/$false
    $filewatcher.IncludeSubdirectories = $true
    $filewatcher.EnableRaisingEvents = $true  
### DEFINE ACTIONS AFTER AN EVENT IS DETECTED
    $global:first_time_fix = $true
    $writeaction = { $path = $Event.SourceEventArgs.FullPath
                #Write-Host "." 

                if ($global:first_time_fix) { 
                    #Write-Host "." 
                    #Write-Host "." 
                    $global:first_time_fix = $false
                }
                $changeType = $Event.SourceEventArgs.ChangeType
                $formattedTimestamp = $(Get-Date).ToString("yyyy-MM-dd HH:mm:ss.fffffff")
                $logline = "$formattedTimestamp, $changeType, $path"
                #Add-content "C:\D_EMS Drive\Personal\LBLOG\FileWatcher_log.txt" -value $logline
                [Environment]::NewLine
                Write-Host $logline
                [Environment]::NewLine
              }    
### DECIDE WHICH EVENTS SHOULD BE WATCHED 
#The Register-ObjectEvent cmdlet subscribes to events that are generated by .NET objects on the local computer or on a remote computer.
#When the subscribed event is raised, it is added to the event queue in your session. To get events in the event queue, use the Get-Event cmdlet.
    #Register-ObjectEvent $filewatcher "Created" -Action $writeaction
    #Register-ObjectEvent $filewatcher "Changed" -Action $writeaction
    #Register-ObjectEvent $filewatcher "Deleted" -Action $writeaction
    Register-ObjectEvent $filewatcher "Renamed" -Action $writeaction


