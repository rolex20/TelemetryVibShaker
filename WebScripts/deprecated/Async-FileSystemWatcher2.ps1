#Deprecado: No funciono bien

# https://powershell.one/tricks/filesystem/filesystemwatcher

$WatchFile1 = "C:\wamp\www\warthunder\mission_data.json"
$WatchFile2 = "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WebScripts\command.json"
$DownloadUrl1 = "http://localhost/warthunder/dogfight_setup_1.php"
$DownloadPath1 = "C:\MyPrograms\Steam\steamapps\common\War Thunder\UserMissions\AutoDogfight_setup1.blk"
$DownloadUrl2 = "http://localhost/warthunder/viewer_setup_1.php"
$DownloadPath2 = "C:\MyPrograms\Steam\steamapps\common\War Thunder\UserMissions\AutoViewer_setup1.blk"

# specify the paths to the folders you want to monitor:
$Path1 = (Get-Item $WatchFile1).DirectoryName
$Path2 = (Get-Item $WatchFile2).DirectoryName

# specify which files you want to monitor, could be *.txt, etc
$FileFilter11 = (Get-Item $WatchFile1).Name
$FileFilter12 = (Get-Item $WatchFile2).Name

# specify whether you want to monitor subfolders as well:
$IncludeSubfolders = $false

# specify the file or folder properties you want to monitor:
$AttributeFilter = [IO.NotifyFilters]::FileName, [IO.NotifyFilters]::LastWrite 

# specify the type of changes you want to monitor:
$ChangeTypes = [System.IO.WatcherChangeTypes]::Changed




# find the path to the desktop folder:
#$WatchFile1 = "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WebScripts"

# specify the path to the folder you want to monitor:
#$Path1 = $desktop

# specify which files you want to monitor
#$FileFilter1 = '*'  

# specify whether you want to monitor subfolders as well:
#$IncludeSubfolders = $true

# specify the file or folder properties you want to monitor:
#$AttributeFilter = [IO.NotifyFilters]::FileName, [IO.NotifyFilters]::LastWrite 

try
{
  $watcher1 = New-Object -TypeName System.IO.FileSystemWatcher -Property @{
    Path = $Path1
    Filter = $FileFilter1
    IncludeSubdirectories = $IncludeSubfolders
    NotifyFilter = $AttributeFilter
  }

  # define the code that should execute when a change occurs:
  $action = {
    # the code is receiving this to work with:
    
    # change type information:
    $details = $event.SourceEventArgs
    $Name = $details.Name
    $FullPath = $details.FullPath
    $OldFullPath = $details.OldFullPath
    $OldName = $details.OldName
    
    # type of change:
    $ChangeType = $details.ChangeType
    
    # when the change occured:
    $Timestamp = $event.TimeGenerated.ToString("yyyy-MM-dd HH:mm:ss.fffffff")
    
    # save information to a global variable for testing purposes
    # so you can examine it later
    # MAKE SURE YOU REMOVE THIS IN PRODUCTION!
    $global:all = $details
    
    # now you can define some action to take based on the
    # details about the change event:
    
    # let's compose a message:
    $text = "{0} was {1} at {2}" -f $FullPath, $ChangeType, $Timestamp
    Write-Host ""
    Write-Host $text -ForegroundColor DarkYellow
    
    # you can also execute code based on change type here:
    switch ($ChangeType)
    {
      'Changed'  { "CHANGE" }
      'Created'  { "CREATED"}
      'Deleted'  { "DELETED"
        # to illustrate that ALL changes are picked up even if
        # handling an event takes a lot of time, we artifically
        # extend the time the handler needs whenever a file is deleted
        Write-Host "Deletion Handler Start" -ForegroundColor Gray
        Start-Sleep -Seconds 4    
        Write-Host "Deletion Handler End" -ForegroundColor Gray
      }
      'Renamed'  { 
        # this executes only when a file was renamed
        $text = "File {0} was renamed to {1}" -f $OldName, $Name
        Write-Host $text -ForegroundColor Yellow
      }
        
      # any unhandled change types surface here:
      default   { Write-Host $_ -ForegroundColor Red -BackgroundColor White }
    }
  }

  # subscribe your event handler to all event types that are
  # important to you. Do this as a scriptblock so all returned
  # event handlers can be easily stored in $handlers:
  $handlers = . {
    Register-ObjectEvent -InputObject $watcher1 -EventName Changed  -Action $action 
    #Register-ObjectEvent -InputObject $watcher1 -EventName Created  -Action $action 
    #Register-ObjectEvent -InputObject $watcher1 -EventName Deleted  -Action $action 
    Register-ObjectEvent -InputObject $watcher1 -EventName Renamed  -Action $action 
  }

  # monitoring starts now:
  $watcher1.EnableRaisingEvents = $true

  Write-Host "Watching for changes to $Path1"

  # since the FileSystemWatcher is no longer blocking PowerShell
  # we need a way to pause PowerShell while being responsive to
  # incoming events. Use an endless loop to keep PowerShell busy:
  do
  {
    # Wait-Event waits for a second and stays responsive to events
    # Start-Sleep in contrast would NOT work and ignore incoming events
    Wait-Event -Timeout 3600

    # write a dot to indicate we are still monitoring:
    Write-Host "." -NoNewline
        
  } while ($true)
}
finally
{
  # this gets executed when user presses CTRL+C:
  
  # stop monitoring
  $watcher1.EnableRaisingEvents = $false
  
  # remove the event handlers
  $handlers | ForEach-Object {
    Unregister-Event -SourceIdentifier $_.Name
  }
  
  # event handlers are technically implemented as a special kind
  # of background job, so remove the jobs now:
  $handlers | Remove-Job
  
  # properly dispose the FileSystemWatcher:
  $watcher1.Dispose()
  
  Write-Warning "Event Handler disabled, monitoring ends."
}