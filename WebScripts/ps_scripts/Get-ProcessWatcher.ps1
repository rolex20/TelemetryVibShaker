. ".\Write-VerboseDebug.ps1"
. ".\Gaming-Programs.ps1"



function Create-ProcessWatcherQuery {
    param (
        [string[]]$ProgramsToMonitor,
        [string]$StartingClause
    )

    $conditions = @()
    foreach ($program in $ProgramsToMonitor) {
        $conditions += "processname = '$program'"
    }

    $query = $StartingClause + ($conditions -join " or ")


    return $query
}


function Get-ProcessWatchers($action) {
        Write-VerboseDebug -Timestamp (Get-Date) -Title "STARTING" -Message "Process Watchers"
        $programs = Get-GamingPrograms

        # Get-ProcessWatcher.ps1
        $startWatcherQuery = Create-ProcessWatcherQuery -ProgramsToMonitor $programs -StartingClause "Select ProcessID, ProcessName, ParentProcessID from win32_ProcessStartTrace where "
        $stopWatcherQuery  = Create-ProcessWatcherQuery -ProgramsToMonitor $programs -StartingClause "Select ProcessID, ProcessName, ParentProcessID from win32_ProcessStopTrace where "
        

        $startWatcher = Register-CimIndicationEvent -Query $startWatcherQuery -SourceIdentifier startSI -Action $action
        $stopWatcher = Register-CimIndicationEvent -Query $stopWatcherQuery -SourceIdentifier stopSI -Action $action

        $retValues = @($startWatcher, $stopWatcher)
        return $retValues
}

