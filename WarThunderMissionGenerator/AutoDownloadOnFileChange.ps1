$EfficiencyAffinity = 983040 # HyperThreading enabled
[System.Diagnostics.Process]::GetCurrentProcess().ProcessorAffinity =  $EfficiencyAffinity

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
$DownloadUrl1 = "http://localhost/warthunder/dogfight_setup_1.php"
$DownloadPath1 = "C:\MyPrograms\Steam\steamapps\common\War Thunder\UserMissions\AutoDogfight_setup1.blk"
$DownloadUrl2 = "http://localhost/warthunder/viewer_setup_1.php"
$DownloadPath2 = "C:\MyPrograms\Steam\steamapps\common\War Thunder\UserMissions\AutoViewer_setup1.blk"
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
$Timeout = 3600*1000

# create a sound player
$sound = New-Object System.Media.SoundPlayer;	

# define a function that gets called for every change:
function Invoke-WebDownload
{
	param
	(
	[Parameter(Mandatory)]
	[System.IO.WaitForChangedResult]
	$ChangeInformation
	)

	$t = Get-Date
	Write-Warning "Change detected: $t"
	Start-Sleep -Milliseconds 300
	$ChangeInformation | Out-String | Write-Host -ForegroundColor DarkYellow
	

	$sound.SoundLocation="C:\Windows\Media\Windows Notify.wav"
	
	try {
		Invoke-WebRequest -Uri $DownloadUrl1 -OutFile $DownloadPath1 -ErrorAction Stop
		Invoke-WebRequest -Uri $DownloadUrl2 -OutFile $DownloadPath2 -ErrorAction Stop
		
		} catch {
			$sound.SoundLocation="C:\Windows\Media\Windows Critical Stop.wav"
	}

	$sound.Play()
}

# use a try...finally construct to release the
# filesystemwatcher once the loop is aborted
# by pressing CTRL+C

try
{  
  
  # create a filesystemwatcher object
  $watcher = New-Object -TypeName IO.FileSystemWatcher -ArgumentList $Path, $FileFilter -Property @{
    IncludeSubdirectories = $IncludeSubfolders
    NotifyFilter = $AttributeFilter
  }

  Write-Warning "FileSystemWatcher is monitoring $Path"	  
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
	    Write-Warning "FileSystemWatcher is monitoring $Path"	  	
		continue  # continue jumps to the begining of the do{} loop
	}
	
    Invoke-WebDownload -Change $result
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