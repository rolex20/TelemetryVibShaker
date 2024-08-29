# . "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts\Write-VerboseDebug.ps1" #Don't forget to include this file, when testing individually

function Send-MessageViaPipe {
    param (
        [string]$pipeName,
        [string]$message
    )


    Write-VerboseDebug -Timestamp (Get-Date) -Title "IPC PIPE" -Message "Sending [$message] to pipe [$pipeName]"
    try {
        
        $pipe = New-Object System.IO.Pipes.NamedPipeClientStream(".", $pipeName, [System.IO.Pipes.PipeDirection]::Out)
        $pipe.Connect(1000)

        $writer = New-Object System.IO.StreamWriter($pipe)
        $writer.AutoFlush = $true
        $writer.WriteLine($message)
        $writer.Close()
        $pipe.Close()
        return $true
        
    } catch {
        $err = "Failed to send message: $_"
        Write-VerboseDebug -Timestamp (Get-Date) -Title "Send-Message-Via-Pipe" -Message "$err" -ForegroundColor "Red"
        return $false
    } finally {
        $pipe.Dispose()
    }
}

#You can use this function like this:

#Send-MessageViaPipe -pipeName "MyPipe" -message "Hello, World!"

#Send-MessageViaPipe -pipeName "WarThunderExporterPipeCommands" -message "MONITOR"
#Send-MessageViaPipe -pipeName "WarThunderExporterPipeCommands" -message "CYCLE_STATISTICS"

#Send-MessageViaPipe -pipeName "TelemetryVibShakerPipeCommands" -message "CYCLE_STATISTICS"

#Send-MessageViaPipe -pipeName "ipc_pipe_vr_server_commands" -message "ECHO"