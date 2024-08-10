#watchdog: Make sure the file system watcher is still working properly

function Get-NewFileName {
    param (
        [string]$FilePath,
        [string]$NewExtension
    )

    # Extract the directory, filename without extension, and current extension
    $directory = [System.IO.Path]::GetDirectoryName($FilePath)
    $filenameWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($FilePath)

    # Combine the directory, filename without extension, and new extension to form the new filename
    $newFileName = [System.IO.Path]::Combine($directory, "$filenameWithoutExtension.$NewExtension")

    return $newFileName
}


function Watchdog_Operations {
    $watchdog_json = Include-Script "watchdog.json" "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\ps_scripts" "C:\Users\ralch"
    $tmp_json = Get-NewFileName -FilePath $command_file -NewExtension "tmp"


    $watchers_OK = $true
    do
    {
        # Wait-Event waits for a second and stays responsive to events
        # Start-Sleep in contrast would NOT work and ignore incoming events

        Wait-Event -Timeout 60


        if (Test-Path "watchdog.txt") { Remove-Item "watchdog.txt" }
        if (Test-Path $command_file) { Remove-Item $command_file }
        Copy-Item $watchdog_json $tmp_json
        Rename-Item $tmp_json $command_file

        Start-Sleep -Milliseconds 100
        if (Test-Path "watchdog.txt") {
            # We are good
        } else {
            Write-VerboseDebug -Timestamp (Get-Date) -Title "ERROR" -Message "Events are not being processed" -ForegroundColor "Red"
            $watchers_OK = $false
            $need_restart = $true
        }
        
    } while ($watchers_OK)
}