function Get-NewFileName {
    param (
        [string]$FilePath,
        [string]$NewExtension
    )

    $directory = [System.IO.Path]::GetDirectoryName($FilePath)
    $filenameWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($FilePath)
    return [System.IO.Path]::Combine($directory, "$filenameWithoutExtension.$NewExtension")
}

function Watchdog_Operations {
    param(
        [string]$CommandFilePath = ((Get-TvsConfig).paths.commandJson)
    )

    $watchdog_json = Join-Path $PSScriptRoot 'watchdog.json'
    $tmp_json = Get-NewFileName -FilePath $CommandFilePath -NewExtension "tmp"

    $failure = $false
    $watchers_OK = $true
    $wait_minutes = 10
    $wait_seconds = 60 * $wait_minutes
    $additional_sleep = 10

    do {
        if ($Global:Watcher_Continue -eq $false) { return $false }

        $found = $false
        Get-Event -SourceIdentifier "DoWatchDogCheck" -ErrorAction SilentlyContinue | ForEach-Object {
            $additional_sleep = $_.MessageData
            Remove-Event -EventId $_.EventIdentifier -ErrorAction SilentlyContinue
            $found = $true
        }

        if ($found) {
            if ($additional_sleep -eq 0) { $additional_sleep = 5 }
            Write-VerboseDebug -Timestamp (Get-Date) -Title "INFO" -Message "Starting a Watchdog sanity check in $additional_sleep seconds..." -ForegroundColor "DarkGray"
        }

        $remainingSleep = [double]$additional_sleep
        while ($remainingSleep -gt 0) {
            if ($Global:Watcher_Continue -eq $false) { return $false }

            $chunk = [Math]::Min(0.5, $remainingSleep)
            Wait-Event -Timeout $chunk | Out-Null

            $stopEvents = Get-Event -SourceIdentifier "StopWatcher" -ErrorAction SilentlyContinue
            if ($stopEvents) {
                $stopEvents | ForEach-Object { Remove-Event -EventId $_.EventIdentifier -ErrorAction SilentlyContinue }
                return $false
            }

            $manualEvents = Get-Event -SourceIdentifier "DoWatchDogCheck" -ErrorAction SilentlyContinue
            if ($manualEvents) {
                $manualEvents | ForEach-Object { Remove-Event -EventId $_.EventIdentifier -ErrorAction SilentlyContinue }
            }

            $remainingSleep -= $chunk
        }

        if (Test-Path "watchdog.txt") { Remove-Item "watchdog.txt" }
        if (Test-Path $CommandFilePath) { Remove-Item $CommandFilePath }
        Copy-Item $watchdog_json $tmp_json
        Rename-Item $tmp_json $CommandFilePath

        Start-Sleep -Milliseconds 200
        if (-not (Test-Path "watchdog.txt")) {
            Write-VerboseDebug -Timestamp (Get-Date) -Title "ERROR" -Message "Events are not being processed" -ForegroundColor "Red"
            $watchers_OK = $false
            $failure = $true
            break
        }

        Wait-Event -Timeout $wait_seconds | Out-Null

        $stopEvents = Get-Event -SourceIdentifier "StopWatcher" -ErrorAction SilentlyContinue
        if ($stopEvents) {
            $stopEvents | ForEach-Object { Remove-Event -EventId $_.EventIdentifier -ErrorAction SilentlyContinue }
            return $false
        }

        $additional_sleep = 0
    } while ($watchers_OK)

    return $failure
}
