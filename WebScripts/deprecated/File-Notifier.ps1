# Deprecated: No funciono bien

# Define the path to watch
$path = "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\warthunder\ps_scripts"

# Create a new FileSystemWatcher
$watcher = New-Object System.IO.FileSystemWatcher
$watcher.Path = $path
$watcher.Filter = "command.json"
$watcher.NotifyFilter = [System.IO.NotifyFilters]::FileName, [System.IO.NotifyFilters]::LastWrite

# Define the action to take when an event is triggered
$action = {
        $eventType = $Event.SourceEventArgs.ChangeType
        #Write-Host " " 

        $currentTimestamp = (Get-Date).ToString("HH:mm:ss.fffffff")

        if ($eventType -eq [System.IO.WatcherChangeTypes]::Created) { $msg = "Creation-Event triggered at: $currentTimestamp"}
        elseif ($eventType -eq [System.IO.WatcherChangeTypes]::Changed) { $msg = "Modification-Event triggered at: $currentTimestamp"}
        elseif ($eventType -eq [System.IO.WatcherChangeTypes]::Renamed) { $msg = "Renamed-Event triggered at: $currentTimestamp"}
        else { $msg = "Unknknown-Event triggered at: $currentTimestamp"}
        Write-Host " "
        Write-Host $msg

        Start-Sleep -Milliseconds 200
        $currentTimestamp = (Get-Date).ToString("HH:mm:ss.fffffff")

#    if ($eventType -eq [System.IO.WatcherChangeTypes]::Changed) {
#    if ((Test-Path -Path $Event.SourceEventArgs.FullPath) -AND ($eventType -eq [System.IO.WatcherChangeTypes]::Changed) ) {
    if ((Test-Path -Path $Event.SourceEventArgs.FullPath) ) {
     
        #$watcher.EnableRaisingEvents = $false
        
        
        # Read and parse the JSON file
        $jsonContent = Get-Content -Path $Event.SourceEventArgs.FullPath -Raw | ConvertFrom-Json

        #$watcher.EnableRaisingEvents = $false

        Remove-Item $Event.SourceEventArgs.FullPath

        $keyCount = $jsonContent.PSObject.Properties.Count        
        
        Write-Host "[$currentTimestamp]Number of keys in JSON file: [$keyCount]"
        #Write-Host "."

        
        #$watcher.EnableRaisingEvents = $false
    }
}

# Register the event handlers with identifiers
$createdEvent = Register-ObjectEvent -InputObject $watcher -EventName "Created" -Action $action -SourceIdentifier "FileCreated"
$changedEvent = Register-ObjectEvent -InputObject $watcher -EventName "Changed" -Action $action -SourceIdentifier "FileChanged"

# Start watching
$watcher.EnableRaisingEvents = $true

# Keep the script running
#Write-Host "Watching for changes to command.json. Press [Enter] to exit."
#[Console]::ReadLine()

# Unregister the event handlers and dispose of the watcher
#Unregister-Event -SourceIdentifier "FileCreated"
#Unregister-Event -SourceIdentifier "FileChanged"
#$watcher.Dispose()

Write-Host "Running"
