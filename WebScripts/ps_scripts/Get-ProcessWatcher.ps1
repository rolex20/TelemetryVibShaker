. "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts\Include-Script.ps1"

$search_paths = @("C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts", "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts")

$include_file = Include-Script -FileName "Gaming-Programs.ps1" -Directories $search_paths
. $include_file


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

