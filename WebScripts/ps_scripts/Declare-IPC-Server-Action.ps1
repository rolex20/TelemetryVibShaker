$ipc_job_action = {
    # Define the named pipe
    $pipeName = "ipc_pipe_vr_server_commands"
    $maxInstances = 2  # Set the maximum number of instances

    # Create the named pipe server
    $pipeServer = New-Object System.IO.Pipes.NamedPipeServerStream($pipeName, [System.IO.Pipes.PipeDirection]::InOut, $maxInstances)

    # Variable to control the server loop
    $global:IPC_ContinueServer = $true

    # ---------------------------------------------------------
    # OPTIMIZATION: Initialize TTS Engine ONCE at startup
    # ---------------------------------------------------------
    Add-Type -AssemblyName System.speech
    $tts = New-Object System.Speech.Synthesis.SpeechSynthesizer
    
    try {
        $tts.SelectVoiceByHints([System.Speech.Synthesis.VoiceGender]::Female, [System.Speech.Synthesis.VoiceAge]::Adult)
    } catch {
        Write-Host "Warning: Requested voice not found, using default."
    }
    $tts.Rate = 0	

# Import necessary functions from user32.dll
Add-Type @"
    using System;
    using System.Runtime.InteropServices;
    public class IPC_User32 {
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

        # ---------------------------------------------------------
        # HANDLE "SPEAK" COMMAND
        # We check this before the switch because it contains dynamic text
        # ---------------------------------------------------------
        if ($command.StartsWith("SPEAK: ")) {
            $textToSay = $command.Substring(7) # Remove "SPEAK: " prefix
            
            # Use SpeakAsync so we don't block the pipe listener
            $tts.SpeakAsync($textToSay) | Out-Null
            return
        }		

        switch ($command) {
            "MINIMIZE" {
                [IPC_User32]::ShowWindow($hwnd, 6)  # SW_MINIMIZE
            }
            "MAXIMIZE" {
                [IPC_User32]::ShowWindow($hwnd, 3)  # SW_MAXIMIZE
            }
            "RESTORE" {
                [IPC_User32]::ShowWindow($hwnd, 9)  # SW_RESTORE
            }
            "SHOW_PROCESS" {

                    # Get the updated list of processes
                    $updatedProcesses = Get-Process | Select-Object Id, Name, CPU
                
                    # Calculate the CPU time differences and filter for positive differences
                    $processesDiff = $updatedProcesses | ForEach-Object {
                        $currentProcess = $_
                        $initialProcess = $initialProcesses | Where-Object { $_.Id -eq $currentProcess.Id }
                        if ($initialProcess) {
                            $diff = $currentProcess.CPU - $initialProcess.CPU
                            if ($diff -gt 0) {
                                [PSCustomObject]@{
                                    Name = $currentProcess.Name
                                    Id = $currentProcess.Id
                                    OldCPUTime = $initialProcess.CPU
                                    NewCPUTime = $currentProcess.CPU
                                    Difference = $diff
                                }
                            }
                        } else {
                                [PSCustomObject]@{
                                    Name = $currentProcess.Name
                                    Id = $currentProcess.Id
                                    OldCPUTime = 0
                                    NewCPUTime = $currentProcess.CPU
                                    Difference = $currentProcess.CPU
                                }			
                        }
                    }
                
                
                # Display the filtered list of processes with positive CPU time differences
                Write-Host "Processes with positive CPU time differences:"
                
                # Sort the processes by the Difference column in descending order
                $sortedProcessesDiff = $processesDiff | Sort-Object -Property Difference -Descending
                
                # Display the sorted list in a table format
                $sortedProcessesDiff | Format-Table -Property Name, Id, OldCPUTime, NewCPUTime, Difference -AutoSize | Out-String | ForEach-Object { Write-Host $_}
                
            }
            "ECHO" {
                # Respond with "ECHO"
                $writer.WriteLine("ECHO")
            }
            "EXIT" {
                # Exit the server loop
                $global:IPC_ContinueServer = $false
            }
            default {
                Write-Output "Unknown command: $command"
            }
        }
    }


    # First, make sure this is the only instance running.

    # Define the name of the mutex, to prevent other instances
    $mutexName = "ipc_pipe_vr_server_mutex"

    # Initialize the variable that will be used to check if the mutex is new
    $isNew = $false

    # Create the Mutex
    $mutex = New-Object System.Threading.Mutex($false, $mutexName, [ref]$isNew)

    if (-NOT $isNew) { 
        Write-Host -ForegroundColor Red "There is another instance of this IPC-Pipe-Server already running."
        Start-Sleep -Seconds 5
        $global:IPC_ContinueServer = $false
    }


    #Write-Host "Starting in 5 minutes.  Open your programs and start working."
    #Start-Sleep -Seconds 300



    # Get the initial list of processes with their CPU times
    $initialProcesses = Get-Process | Select-Object Id, Name, CPU | Sort-Object -Property CPU -Descending

    # Display the initial list of processes
    #Write-Host "Initial list of processes:"
    #$initialProcesses | Format-Table -Property Name, CPU -AutoSize


    # Start the server loop
    while ($global:IPC_ContinueServer) {
        Write-Host "Waiting for new IPC client connection..."
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
    $mutex.Dispose()
	if ($tts) { $tts.Dispose() }
	Write-Host "IPC Pipe Server terminated."
}
