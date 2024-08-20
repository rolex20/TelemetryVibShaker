#Deprecado: No funciono bien

$EfficiencyAffinity = 983040 # HyperThreading enabled
#[System.Diagnostics.Process]::GetCurrentProcess().ProcessorAffinity =  $EfficiencyAffinity

$Host.UI.RawUI.WindowTitle = 'AutoDownloadFileOnChange'

# This script monitors specified files for changes. When a file is modified,
# the script downloads files from provided URLs and saves them to designated paths.

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

$WatchFile1 = "C:\wamp\www\warthunder\mission_data.json"
$WatchFile2 = "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\command.json"
$DownloadUrl1 = "http://localhost/warthunder/dogfight_setup_1.php"
$DownloadPath1 = "C:\MyPrograms\Steam\steamapps\common\War Thunder\UserMissions\AutoDogfight_setup1.blk"
$DownloadUrl2 = "http://localhost/warthunder/viewer_setup_1.php"
$DownloadPath2 = "C:\MyPrograms\Steam\steamapps\common\War Thunder\UserMissions\AutoViewer_setup1.blk"

# specify the paths to the folders you want to monitor:
$Path1 = (Get-Item $WatchFile1).DirectoryName
$Path2 = (Get-Item $WatchFile2).DirectoryName

# specify which files you want to monitor, could be *.txt, etc
$FileFilter1 = (Get-Item $WatchFile1).Name
$FileFilter2 = (Get-Item $WatchFile2).Name

# specify whether you want to monitor subfolders as well:
$IncludeSubfolders = $false

# specify the file or folder properties you want to monitor:
$AttributeFilter = [IO.NotifyFilters]::FileName, [IO.NotifyFilters]::LastWrite 

# specify the type of changes you want to monitor:
$ChangeTypes = [System.IO.WatcherChangeTypes]::Changed

# create a sound player
$sound = New-Object System.Media.SoundPlayer;    

# define a function that gets called for every change:
function Invoke-WebDownload
{
    param
    (
        [Parameter(Mandatory)]
        [System.IO.FileSystemEventArgs]
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


    # create filesystemwatcher objects
    $watcher1 = New-Object -TypeName System.IO.FileSystemWatcher
    $watcher1.Path = $Path1
    $watcher1.Filter = $FileFilter1
    $watcher1.IncludeSubdirectories = $IncludeSubfolders
    $watcher1.NotifyFilter = $AttributeFilter

    $watcher2 = New-Object -TypeName System.IO.FileSystemWatcher
    $watcher2.Path = $Path2
    $watcher2.Filter = $FileFilter2
    $watcher2.IncludeSubdirectories = $IncludeSubfolders
    $watcher2.NotifyFilter = $AttributeFilter

    # Register event handlers
    $action = {
        param($source, $eventArgs)
        Invoke-WebDownload -ChangeInformation $eventArgs
    }

    $handlers1 = . {
        Register-ObjectEvent -InputObject $watcher1 -EventName Changed -Action $action
    }

    $handlers2 = . {
        Register-ObjectEvent -InputObject $watcher2 -EventName Changed -Action $action
    }

    # Start monitoring
    $watcher1.EnableRaisingEvents = $true
    $watcher2.EnableRaisingEvents = $true

    Write-Warning "FileSystemWatcher is monitoring $Path1 and $Path2"      

