function Show-CPU-Time-PerProcess {
    param([int]$AffinityMask = 983040)

    Try-SetCurrentProcessAffinity -Mask $AffinityMask -ContextTitle 'Show-CPU-Time-PerProcess' | Out-Null

    $initialProcesses = Get-Process | Select-Object Id, Name, CPU | Sort-Object CPU -Descending
    Write-Host 'Initial list of processes:'
    $initialProcesses | Format-Table -Property Name, CPU -AutoSize

    while ($true) {
        $updatedProcesses = Get-Process | Select-Object Id, Name, CPU
        $processesDiff = $updatedProcesses | ForEach-Object {
            $currentProcess = $_
            $initialProcess = $initialProcesses | Where-Object { $_.Id -eq $currentProcess.Id }
            if ($initialProcess) {
                $diff = $currentProcess.CPU - $initialProcess.CPU
                if ($diff -gt 0) { [PSCustomObject]@{Name=$currentProcess.Name;Id=$currentProcess.Id;OldCPUTime=$initialProcess.CPU;NewCPUTime=$currentProcess.CPU;Difference=$diff} }
            } else {
                [PSCustomObject]@{Name=$currentProcess.Name;Id=$currentProcess.Id;OldCPUTime=0;NewCPUTime=$currentProcess.CPU;Difference=$currentProcess.CPU}
            }
        }

        Write-Host 'Processes with positive CPU time differences:'
        $processesDiff | Sort-Object Difference -Descending | Format-Table Name, Id, OldCPUTime, NewCPUTime, Difference -AutoSize
        Read-Host 'Press Ctrl+C to Exit...'
    }
}
