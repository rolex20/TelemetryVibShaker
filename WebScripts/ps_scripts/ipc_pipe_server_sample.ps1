
# Define the named pipe
$pipeName = "ipc_pipe_vr_server_commands"
$maxInstances = 2  # Set the maximum number of instances

# Create the named pipe server
$pipeServer = New-Object System.IO.Pipes.NamedPipeServerStream($pipeName, [System.IO.Pipes.PipeDirection]::InOut, $maxInstances)

# Variable to control the server loop
$global:IPC_serverRunning = $true

# Import necessary functions from user32.dll
Add-Type @"
    using System;
    using System.Runtime.InteropServices;
    public class User32 {
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
"@

# Function to handle commands
function Handle-Command {
    param (
        [string]$command,
        [System.IO.StreamWriter]$writer
    )

    $hwnd = (Get-Process -Id $pid).MainWindowHandle

    switch ($command) {
        "MINIMIZE" {
            [User32]::ShowWindow($hwnd, 6)  # SW_MINIMIZE
        }
        "MAXIMIZE" {
            [User32]::ShowWindow($hwnd, 3)  # SW_MAXIMIZE
        }
        "RESTORE" {
            [User32]::ShowWindow($hwnd, 9)  # SW_RESTORE
        }
        "SHOW_PROCESS" {
            # Show the list of running processes
            Get-Process | Format-Table -AutoSize
        }
        "ECHO" {
            # Respond with "ECHO"
            $writer.WriteLine("ECHO")
        }
        "EXIT" {
            # Exit the server loop
            $global:IPC_serverRunning = $false
        }
        default {
            Write-Output "Unknown command: $command"
        }
    }
}

# Start the server loop
while ($global:IPC_serverRunning) {
    Write-Output "Waiting for client connection..."
    $pipeServer.WaitForConnection()

    $reader = New-Object System.IO.StreamReader($pipeServer)
    $writer = New-Object System.IO.StreamWriter($pipeServer)
    $writer.AutoFlush = $true

    try {
        while ($pipeServer.IsConnected) {
            $command = $reader.ReadLine()
            if ($command) {
                Handle-Command -command $command -writer $writer
            }
        }
    } finally {
        $pipeServer.Disconnect()
    }
}
