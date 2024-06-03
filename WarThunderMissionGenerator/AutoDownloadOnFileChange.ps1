$Host.UI.RawUI.WindowTitle = 'AutoDownloadFileOnChange'

# This script monitors a specified file for changes. When the file is modified,
# the script downloads a file from a provided URL and saves it to a designated path.
# The script then sleeps for a set duration and repeats the process, waiting
# for the next modification on the watch file.


# First, make sure this is the only instance running.

# Define the name of the mutex, to prevent other instances
$mutexName = "Global\AutoDownloadOnFileChange"

# Initialize the variable that will be used to check if the mutex is new
$isNew = $false

# Create the Mutex
$mutex = New-Object System.Threading.Mutex($false, $mutexName, [ref]$isNew)

if (-NOT $isNew) { 
	Write-Host -ForegroundColor Red "There is another instance of this script already running."
	Start-Sleep -Seconds 5
	Exit 
}

$WatchFile = "C:\MyPrograms\wamp\www\warthunder\mission_data.json"
$DownloadUrl = "http://localhost/warthunder/generate_mission.php"
$DownloadPath = "C:\MyPrograms\Steam\steamapps\common\War Thunder\UserMissions\AutoMission.blk"

# specify the path to the folder you want to monitor:
$Path =  (Get-Item $WatchFile).DirectoryName

# specify which files you want to monitor, could be *.txt, etc
$FileFilter = (Get-Item $WatchFile).Name

# specify whether you want to monitor subfolders as well:
$IncludeSubfolders = $false

# specify the file or folder properties you want to monitor:
$AttributeFilter = [IO.NotifyFilters]::FileName, [IO.NotifyFilters]::LastWrite 

# specify the type of changes you want to monitor:
$ChangeTypes = [System.IO.WatcherChangeTypes]::Changed

# specify the maximum time (in milliseconds) you want to wait for changes:
$Timeout = 1000*3600*24

# define a function that gets called for every change:
function Invoke-SomeAction
{
  param
  (
    [Parameter(Mandatory)]
    [System.IO.WaitForChangedResult]
    $ChangeInformation
  )
  
  $t = Get-Date
  Write-Warning "Change detected: $t"
  Start-Sleep -Seconds 1
  $ChangeInformation | Out-String | Write-Host -ForegroundColor DarkYellow

  Invoke-WebRequest -Uri $DownloadUrl -OutFile $DownloadPath
}

# use a try...finally construct to release the
# filesystemwatcher once the loop is aborted
# by pressing CTRL+C

try
{
  Write-Warning "FileSystemWatcher is monitoring $Path"
  
  # create a filesystemwatcher object
  $watcher = New-Object -TypeName IO.FileSystemWatcher -ArgumentList $Path, $FileFilter -Property @{
    IncludeSubdirectories = $IncludeSubfolders
    NotifyFilter = $AttributeFilter
  }

  # start monitoring manually in a loop:
  do
  {
    # wait for changes for the specified timeout
    # IMPORTANT: while the watcher is active, PowerShell cannot be stopped
    # so change the timeout to 1000ms and repeat the
    # monitoring in a loop if you want to have the chance to abort the
    # script every second.
    $result = $watcher.WaitForChanged($ChangeTypes, $Timeout)
    # if there was a timeout, continue monitoring:
    if ($result.TimedOut) { 
		continue  # continue jumps to the begining of the do{} loop
	}
	
    Invoke-SomeAction -Change $result
    # the loop runs forever until you hit CTRL+C    
  } while ($true)
}
finally
{
  # release the watcher and free its memory:
  Write-Warning 'Disposing FileSystemWatcher.'  
  $watcher.Dispose()
  
  # Release the mutex
  $mutex.ReleaseMutex()
  $mutex.Close()  
}