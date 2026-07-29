$scriptDir = $PSScriptRoot
. (Join-Path $scriptDir 'Write-VerboseDebug.ps1')
. (Join-Path $scriptDir 'Gaming-Programs.ps1')

function Create-ProcessWatcherQuery {
    param (
        [string[]]$ProgramsToMonitor,
        [string]$StartingClause
    )

    $conditions = @()
    foreach ($program in $ProgramsToMonitor) {
        # Query is intentionally explicit OR conditions per process name.
        # We control these names via GameProfiles, so direct string interpolation is acceptable here.
        # If this ever accepts untrusted input, switch to stricter escaping/validation.
        $conditions += "processname = '$program'"
    }

    $query = $StartingClause + ($conditions -join " or ")


    return $query
}


function Get-ProcessWatchers {
    param(
        [Parameter(Mandatory)]
        [scriptblock]$Action,

        $MessageData
    )

        Write-VerboseDebug -Timestamp (Get-Date) -Title "STARTING" -Message "Process Watchers"
        $programs = Get-GamingPrograms

        # Get-ProcessWatcher.ps1
        $startWatcherQuery = Create-ProcessWatcherQuery -ProgramsToMonitor $programs -StartingClause "Select ProcessID, ProcessName, ParentProcessID from win32_ProcessStartTrace where "
        $stopWatcherQuery  = Create-ProcessWatcherQuery -ProgramsToMonitor $programs -StartingClause "Select ProcessID, ProcessName, ParentProcessID from win32_ProcessStopTrace where "
        

        # These source identifiers are stable names used elsewhere for bulk event cleanup.
        # If multiple watcher instances were ever allowed in one process, these would collide.
        # Current orchestrator design prevents that via single-instance mutex.
        # The same MessageData object reference is supplied to both subscriptions. This is
        # how start and stop callbacks share exact auxiliary ownership state safely.
        $startWatcher = Register-CimIndicationEvent -Query $startWatcherQuery -SourceIdentifier startSI -Action $Action -MessageData $MessageData
        $stopWatcher = Register-CimIndicationEvent -Query $stopWatcherQuery -SourceIdentifier stopSI -Action $Action -MessageData $MessageData

        # Return subscribers so caller can explicitly dispose/unregister in finally{}.
        # Relying only on process-exit cleanup makes restarts noisier and less deterministic.
        $retValues = @($startWatcher, $stopWatcher)
        return $retValues
}

