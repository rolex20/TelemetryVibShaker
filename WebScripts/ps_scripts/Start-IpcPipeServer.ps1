function Start-IpcPipeServer {
    param(
        [string]$PipeName = 'ipc_pipe_vr_server_commands',
        [string]$MutexName = 'ipc_pipe_vr_server_mutex'
    )

    $pipeSecurity = New-Object System.IO.Pipes.PipeSecurity
    $everyoneSid = New-Object System.Security.Principal.SecurityIdentifier([System.Security.Principal.WellKnownSidType]::WorldSid, $null)
    $accessRule = New-Object System.IO.Pipes.PipeAccessRule($everyoneSid, [System.IO.Pipes.PipeAccessRights]::ReadWrite, [System.Security.AccessControl.AccessControlType]::Allow)
    $pipeSecurity.AddAccessRule($accessRule)
    $pipeServer = New-Object System.IO.Pipes.NamedPipeServerStream($PipeName, [System.IO.Pipes.PipeDirection]::InOut, 2, [System.IO.Pipes.PipeTransmissionMode]::Byte, [System.IO.Pipes.PipeOptions]::Asynchronous, 0, 0, $pipeSecurity)

    Add-Type -AssemblyName System.speech
    $tts = New-Object System.Speech.Synthesis.SpeechSynthesizer
    $global:IPC_ContinueServer = $true
    $isNew = $false
    $mutex = New-Object System.Threading.Mutex($false, $MutexName, [ref]$isNew)
    if (-not $isNew) { $global:IPC_ContinueServer = $false }

    try {
        while ($global:IPC_ContinueServer) {
            $pipeServer.WaitForConnection()
            $reader = New-Object System.IO.StreamReader($pipeServer)
            $writer = New-Object System.IO.StreamWriter($pipeServer)
            $writer.AutoFlush = $true
            try {
                while ($pipeServer.IsConnected) {
                    $command = $reader.ReadLine()
                    if ($command -eq 'ECHO') { $writer.WriteLine('ECHO') }
                    elseif ($command -like 'SPEAK *') { $tts.SpeakAsync($command.Substring(6)) | Out-Null }
                    elseif ($command -eq 'EXIT') { $global:IPC_ContinueServer = $false }
                }
            } finally {
                $pipeServer.Disconnect()
            }
        }
    } finally {
        $mutex.Dispose()
        $pipeServer.Dispose()
        if ($tts) { $tts.Dispose() }
    }
}
