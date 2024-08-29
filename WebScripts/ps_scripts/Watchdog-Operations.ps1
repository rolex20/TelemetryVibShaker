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
    $watchdog_json = Include-Script "watchdog.json" "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts" "C:\Users\ralch"
    $tmp_json = Get-NewFileName -FilePath $command_file -NewExtension "tmp"

	$failure = $false
    $watchers_OK = $true
	

    # CHANGE HERE FOR DIFFERENT WAIT TIME
	$wait_minutes = 10 #Change this	to customize
	$wait_seconds = 60 * $wait_minutes


    $additional_sleep = 10
    do
    {
        # Wait-Event waits while staying responsive to events
        # Start-Sleep in contrast would NOT work and ignore incoming events
		
		$found = $false
        #Remove manual events if any
		Get-Event -SourceIdentifier "DoWatchDogCheck" -ErrorAction SilentlyContinue | ForEach-Object { $additional_sleep = $_.MessageData ; Remove-Event -EventId $_.EventIdentifier -ErrorAction SilentlyContinue ; $found = $true}
		if ($found) {
            if ($additional_sleep -EQ 0) { $additional_sleep = 5 } # if foreach failed
			Write-VerboseDebug -Timestamp (Get-Date) -Title "INFO" -Message "Starting a Watchdog sanity check in $additional_sleep seconds..." -ForegroundColor "DarkGray"
		} 
		Start-Sleep -Seconds $additional_sleep #Allow some time of rest from the previous command

        
        if (Test-Path "watchdog.txt") { Remove-Item "watchdog.txt" }
        if (Test-Path $command_file) { Remove-Item $command_file }
        Copy-Item $watchdog_json $tmp_json
        Rename-Item $tmp_json $command_file

        Start-Sleep -Milliseconds 200
        if (Test-Path "watchdog.txt") {
            # We are good
        } else {
            Write-VerboseDebug -Timestamp (Get-Date) -Title "ERROR" -Message "Events are not being processed" -ForegroundColor "Red"
            $watchers_OK = $false
			$failure = $true
            break;
        }

		Wait-Event -Timeout $wait_seconds # Check every five minutes or when a manual event has been signaled        

        $additional_sleep = 0
    } while ($watchers_OK)
	
	return $failure
}