# . "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts\Write-VerboseDebug.ps1" #Don't forget to include this file, when testing individually

function Send-MessageViaPipe {
    param (
        [string]$pipeName,
        [string]$message
    )


    Write-VerboseDebug -Timestamp (Get-Date) -Title "IPC PIPE" -Message "$pipeName $message"
    try {
        
        $pipe = New-Object System.IO.Pipes.NamedPipeClientStream(".", $pipeName, [System.IO.Pipes.PipeDirection]::Out)
        $pipe.Connect(1000)

        $writer = New-Object System.IO.StreamWriter($pipe)
        $writer.AutoFlush = $true
        $writer.WriteLine($message)
        $writer.Close()
        $pipe.Close()

        
    } catch {
        $err = "Failed to send message: $_"
        Write-VerboseDebug -Timestamp (Get-Date) -Title "IPC PIPE" -Message "$err" -ForegroundColor "Red"
    }
}

#You can use this function like this:

#Send-MessageViaPipe -pipeName "MyPipe" -message "Hello, World!"

#Send-MessageViaPipe -pipeName "WarThunderExporterPipeCommands" -message "MONITOR"
#Send-MessageViaPipe -pipeName "WarThunderExporterPipeCommands" -message "CYCLE_STATISTICS"

#Send-MessageViaPipe -pipeName "TelemetryVibShakerPipeCommands" -message "CYCLE_STATISTICS"