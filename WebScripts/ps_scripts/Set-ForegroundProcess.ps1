#. "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\ps_scripts\Write-VerboseDebug.ps1" #Don't forget to include this file for independent testing

function Set-ForegroundProcess {
    param (
        [Parameter(Mandatory)] [string]$processName, 
        [Parameter(Mandatory)] [int]$instance
    )

    # Add necessary assemblies
    Add-Type @"
        using System;
        using System.Runtime.InteropServices;
        public class User32_Foreground {
            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetForegroundWindow(IntPtr hWnd);
        }
"@

    Write-VerboseDebug -Timestamp (Get-Date) -Title "FOREGROUND" -Message $processName
    # Get the process
    $process = Get-Process -Name $processName -ErrorAction SilentlyContinue
    if ($process.Count -ge 1) {
        $process = $process[$instance]        
        # Bring the process to the foreground
        try{
            $hwnd = $process.MainWindowHandle
            $result = [User32_Foreground]::SetForegroundWindow($hwnd)
            if ($result) { Write-VerboseDebug -Timestamp (Get-Date) -Title "FOREGROUND" -Message "SUCCESS" }
        } catch {
            Write-VerboseDebug -Timestamp (Get-Date) -Title "FOREGROUND-ERROR" -Message "SetForegroundWindow() failed for $processName" -ForegroundColor "Red"
        }

    } else {
        Write-VerboseDebug -Timestamp (Get-Date) -Title "FOREGROUND-ERROR" -Message "Process not found: $processName" -ForegroundColor "Red"
    }

}

#You can use this function by calling it with the name of the process you want to bring to the foreground, like this:

#Set-ForegroundProcess -processName "FalconExporter" -instance 0