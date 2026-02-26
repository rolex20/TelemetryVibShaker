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
        $programs = Get-GamingPrograms

        $startWatcherQuery = Create-ProcessWatcherQuery -ProgramsToMonitor $programs -StartingClause "Select ProcessID, ProcessName from win32_ProcessStartTrace where "
        $stopWatcherQuery = Create-ProcessWatcherQuery -ProgramsToMonitor $programs -StartingClause "Select ProcessID, ProcessName from win32_ProcessStopTrace where "

        $startWatcher = Register-CimIndicationEvent -Query $startWatcherQuery -SourceIdentifier startSI -Action $action
        $stopWatcher = Register-CimIndicationEvent -Query $stopWatcherQuery -SourceIdentifier stopSI -Action $action

        $retValues = @($startWatcher, $stopWatcher)
        return $retValues
}

