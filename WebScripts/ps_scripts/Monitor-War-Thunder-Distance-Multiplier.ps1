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
    # Create a new file watcher object.
    $filewatcher = New-Object System.IO.FileSystemWatcher

    # Set the path and filter for the file watcher.
    $filewatcher.Path = [System.IO.Path]::GetDirectoryName($WatchFile)
    $filewatcher.Filter = [System.IO.Path]::GetFileName($WatchFile)

    # Configure the file watcher to only watch the specified file.
    $filewatcher.IncludeSubdirectories = $false

    # Enable event raising for the file watcher.
    $filewatcher.EnableRaisingEvents = $true

    # Register an event handler for the "Changed" event.
    $event = Register-ObjectEvent $filewatcher "Changed" -Action $Action

    # Return the event object and the file watcher object.
    return ($event, $filewatcher)
}



# Example usage: Monitor War Thunder config.blk for changes
$watchFile = "C:\MyPrograms\Steam\steamapps\common\War Thunder\config.blk"

$action = {

            function Convert-Seconds($dt) {
                return [int64](Get-Date($dt) -UFormat %s)
            }

            $path = $Event.SourceEventArgs.FullPath
            $changeType = $Event.SourceEventArgs.ChangeType

            Write-Host " "
            Write-Host "War Thunder change type: $($changeType)"
            Write-Host $Event.TimeGenerated 


            $ut = Convert-Seconds $Event.TimeGenerated

            
            if ($global:ModificationWatcherDebouncer -AND ($global:ModificationWatcherDebouncer -EQ $ut) ) {
                Write-Host "Already Processed"
                return
            } 
            
            $global:ModificationWatcherDebouncer = $ut
            
            Write-Host "CHANGE_TYPE: $changeType" 
            Write-Host "Path: $path"

            # Define the path to the War Thunder config file
            $configFilePath = $path

            # Check if the file exists
            if (-Not (Test-Path $configFilePath)) {
                Write-Host "Config file not found."
                Return
            }

            # Delay 100ms to allow the filesystem to complete the two events firing almost simultaneously
            Start-Sleep -Milliseconds 100

            # Read the config file contents
            $configContent = Get-Content $configFilePath

            # Search for the line containing 'rendinstDistMul' parameter with the correct format 'rendinstDistMul:r=...'
            $distMulLine = $configContent | Where-Object { $_ -match "rendinstDistMul:r=" }

            # Extract the value of the 'rendinstDistMul' parameter
            if ($distMulLine -match 'rendinstDistMul:r=([0-9\.]+)') {
                $distMulValue = $matches[1]
                Write-Host "The current value of rendinstDistMul is: $distMulValue"

                # Use System.Speech.Synthesis.SpeechSynthesizer to "speak" the value
                Add-Type -AssemblyName System.Speech
                $synthesizer = New-Object System.Speech.Synthesis.SpeechSynthesizer
                $synthesizer.Speak("New distance multiplier: $distMulValue")
            } else {
                Write-Host "The parameter 'rendinstDistMul' was not found in the config file."
            }

}

# Start monitoring the file
$event, $filewatcher = Get-ModificationWatcher -WatchFile $watchFile -Action $action

