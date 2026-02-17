$ipc_job_action = {
. "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts\Include-Script.ps1"
$search_paths = @("C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts", "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts")
$include_file = Include-Script -FileName "Write-VerboseDebug.ps1" -Directories $search_paths
. $include_file

    # Define the named pipe
    $pipeName = "ipc_pipe_vr_server_commands"
    $maxInstances = 2  # Set the maximum number of instances

    # Create the named pipe server
    #$pipeServer = New-Object System.IO.Pipes.NamedPipeServerStream($pipeName, [System.IO.Pipes.PipeDirection]::InOut, $maxInstances)



    # 1. Create the Security Object
    $pipeSecurity = New-Object System.IO.Pipes.PipeSecurity

    # 2. Get the "Everyone" SID (WorldSid)
    # This works on all versions of Windows (English, Spanish, etc.)
    $everyoneSid = New-Object System.Security.Principal.SecurityIdentifier([System.Security.Principal.WellKnownSidType]::WorldSid, $null)

    # 3. Create the Access Rule (Grant Everyone Read/Write access)
    $accessRule = New-Object System.IO.Pipes.PipeAccessRule(
        $everyoneSid, 
        [System.IO.Pipes.PipeAccessRights]::ReadWrite, 
        [System.Security.AccessControl.AccessControlType]::Allow
    )
    $pipeSecurity.AddAccessRule($accessRule)

    # 4. Initialize the pipe with the Security Object
    # PS 5.1 requires this specific constructor to apply security at creation
    $pipeServer = New-Object System.IO.Pipes.NamedPipeServerStream(
        $pipeName, 
        [System.IO.Pipes.PipeDirection]::InOut, 
        $maxInstances, 
        [System.IO.Pipes.PipeTransmissionMode]::Byte, 
        [System.IO.Pipes.PipeOptions]::Asynchronous, 
        0, # Default input buffer
        0, # Default output buffer
        $pipeSecurity
    )

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
        # CLEAN PARSING: Verb + Payload
        # Split the string into maximum 2 parts using space as delimiter.
        # Example: "SPEAK Hello World" -> $verb="SPEAK", $payload="Hello World"
        # Example: "MINIMIZE"          -> $verb="MINIMIZE", $payload=$null
        # ---------------------------------------------------------
        $parts = $command -split ' ', 2
        $verb = $parts[0]
        $payload = if ($parts.Count -gt 1) { $parts[1] } else { $null }
		
		Write-VerboseDebug -Timestamp (Get-Date) -Title "IPC COMMAND" -Message $command
		
        switch ($verb) {
            "SPEAK" {
                if ($payload) {
                    $tts.SpeakAsync($payload) | Out-Null
                }
            }		
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
                Write-Output "Unknown command: $verb"
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
		Write-VerboseDebug -Timestamp (Get-Date) -Title "IPC SERVER" -Message "Listening on pipe [$pipeName]"
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
	Write-VerboseDebug -Timestamp (Get-Date) -Title "IPC EXIT" -Message "IPC Pipe Server terminated."
}
