# Define the named pipe
$pipeName = "ipc_pipe_vr_server_commands"

# Function to send a command to the server
function Send-Command {
    param (
        [string]$command
    )

	Write-Output "SERVER PIPE NAME: [$pipeName]"
    try {
        $pipeClient = New-Object System.IO.Pipes.NamedPipeClientStream(".", $pipeName, [System.IO.Pipes.PipeDirection]::InOut)
        $pipeClient.Connect(1000) # Timeout after 1 second if the server is not available

        $writer = New-Object System.IO.StreamWriter($pipeClient)
        $writer.AutoFlush = $true

        $reader = New-Object System.IO.StreamReader($pipeClient)

        $writer.WriteLine($command)
        
        if ($command -eq "ECHO") {
            $response = $reader.ReadLine()
            Write-Output "Server response: $response"
        }

        $pipeClient.Close()
    } catch {
        Write-Host "Failed to connect to the server. Make sure the server is running. $($_.Exception.GetType().FullName) - $($_.Exception.Message)"   -ForegroundColor Red

    }
}

# Main loop to prompt for commands
while ($true) {
    $command = Read-Host "Enter command (MINIMIZE, MAXIMIZE, RESTORE, SHOW_PROCESS, ECHO, EXIT, SPEAK <something>), QUIT"
    if ($command -eq "QUIT") {
        break
    }	
    Send-Command -command $command

}
