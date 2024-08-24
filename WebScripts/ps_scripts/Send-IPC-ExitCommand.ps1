function Send-IPC-ExitCommand {
    param (
        [string]$pipeName
    )

    try {
        $pipeClient = New-Object System.IO.Pipes.NamedPipeClientStream(".", $pipeName, [System.IO.Pipes.PipeDirection]::InOut)
        $pipeClient.Connect(1000) # Timeout after 1 second if the server is not available

        $writer = New-Object System.IO.StreamWriter($pipeClient)
        $writer.AutoFlush = $true

        $writer.WriteLine("EXIT")
        $pipeClient.Close()
        Write-Host "EXIT command sent successfully."
        return $true
    } catch {
        Write-Host "Failed to connect to the server. Make sure the server is running."
        return $false
    }
}