function Start-IpcPipeServer {
    param(
        [Parameter(Mandatory=$true)][string]$PipeName,
        [Parameter(Mandatory=$true)][string]$MutexName
    )

    $maxInstances = 2
    $global:IPC_ContinueServer = $true
    $initialProcesses = Get-Process | Select-Object Id, Name, CPU | Sort-Object -Property CPU -Descending

    $pipeSecurity = New-Object System.IO.Pipes.PipeSecurity
    $everyoneSid = New-Object System.Security.Principal.SecurityIdentifier([System.Security.Principal.WellKnownSidType]::WorldSid, $null)
    $accessRule = New-Object System.IO.Pipes.PipeAccessRule($everyoneSid,[System.IO.Pipes.PipeAccessRights]::ReadWrite,[System.Security.AccessControl.AccessControlType]::Allow)
    $pipeSecurity.AddAccessRule($accessRule)

    Add-Type -AssemblyName System.speech
    $tts = New-Object System.Speech.Synthesis.SpeechSynthesizer
    try { $tts.SelectVoiceByHints([System.Speech.Synthesis.VoiceGender]::Female, [System.Speech.Synthesis.VoiceAge]::Adult) } catch {}
    $tts.Rate = 0

    Add-Type @"
using System;
using System.Runtime.InteropServices;
public class IPC_User32 { [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow); }
"@

    $isNew = $false
    $mutex = New-Object System.Threading.Mutex($false, $MutexName, [ref]$isNew)
    if (-not $isNew) {
        Write-VerboseDebug -Timestamp (Get-Date) -Title 'IPC SERVER' -Message 'Another IPC server instance is already running.' -ForegroundColor Red
        $global:IPC_ContinueServer = $false
    }

    try {
        while ($global:IPC_ContinueServer) {
            $pipeServer = New-Object System.IO.Pipes.NamedPipeServerStream($PipeName,[System.IO.Pipes.PipeDirection]::InOut,$maxInstances,[System.IO.Pipes.PipeTransmissionMode]::Byte,[System.IO.Pipes.PipeOptions]::Asynchronous,0,0,$pipeSecurity)
            Write-VerboseDebug -Timestamp (Get-Date) -Title 'IPC SERVER' -Message "Listening on pipe [$PipeName]"
            $pipeServer.WaitForConnection()
            $reader = New-Object System.IO.StreamReader($pipeServer)
            $writer = New-Object System.IO.StreamWriter($pipeServer)
            $writer.AutoFlush = $true
            try {
                while ($pipeServer.IsConnected) {
                    $command = $reader.ReadLine()
                    if (-not $command) { continue }
                    $parts = $command -split ' ', 2
                    $verb = $parts[0]
                    $payload = if ($parts.Count -gt 1) { $parts[1] } else { $null }
                    $hwnd = (Get-Process -Id $pid).MainWindowHandle
                    Write-VerboseDebug -Timestamp (Get-Date) -Title 'IPC COMMAND' -Message $command
                    switch ($verb) {
                        'SPEAK' { if ($payload) { $tts.SpeakAsync($payload) | Out-Null } }
                        'MINIMIZE' { [IPC_User32]::ShowWindow($hwnd, 6) | Out-Null }
                        'MAXIMIZE' { [IPC_User32]::ShowWindow($hwnd, 3) | Out-Null }
                        'RESTORE' { [IPC_User32]::ShowWindow($hwnd, 9) | Out-Null }
                        'SHOW_PROCESS' {
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
                            ($processesDiff | Sort-Object Difference -Descending | Format-Table Name,Id,OldCPUTime,NewCPUTime,Difference -AutoSize | Out-String) | Write-Host
                        }
                        'ECHO' { $writer.WriteLine('ECHO') }
                        'EXIT' { $global:IPC_ContinueServer = $false }
                        default { Write-Output "Unknown command: $verb" }
                    }
                }
            } finally {
                if ($pipeServer.IsConnected) { $pipeServer.Disconnect() }
                $pipeServer.Dispose()
            }
        }
    } finally {
        if ($mutex) { $mutex.Dispose() }
        if ($tts) { $tts.Dispose() }
        Write-VerboseDebug -Timestamp (Get-Date) -Title 'IPC EXIT' -Message 'IPC Pipe Server terminated.'
    }
}
