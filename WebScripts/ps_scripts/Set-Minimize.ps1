#. "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\ps_scripts\Write-VerboseDebug.ps1" #Don't forget to include this file

function Set-Minimize {
    param (
        [Parameter(Mandatory)] [string]$processName, 
        [Parameter(Mandatory)] [int]$instance
    )

    Write-VerboseDebug -Timestamp (Get-Date) -Title "MINIMIZE" -Message $processName

    # Minimize a process window
    $process = Get-Process -Name $processName -ErrorAction SilentlyContinue
    if ($process.Count -ge 1) {
        $process = $process[$instance]
        $hwnd = $process.MainWindowHandle
        if ($hwnd -ne [IntPtr]::Zero) {
            $SW_MINIMIZE = 6
            Add-Type @"
                using System;
                using System.Runtime.InteropServices;
                public class User32_Minimize {
                    [DllImport("user32.dll")]
                    [return: MarshalAs(UnmanagedType.Bool)]
                    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
                }
"@
            
            try {
                $r = [User32_Minimize]::ShowWindow($hwnd, $SW_MINIMIZE)
            } catch {
                Write-VerboseDebug -Timestamp (Get-Date) -Title "MINIMIZE-ERROR" -Message "ShowWindow() failed for $processName" -ForegroundColor "Red"
            }
            } else {
            Write-VerboseDebug -Timestamp (Get-Date) -Title "MINIMIZE-ERROR" -Message "MainWindowHandle failed for: $processName" -ForegroundColor "Red"
            }
    } else {
        Write-VerboseDebug -Timestamp (Get-Date) -Title "MINIMIZE-ERROR" -Message "Process not found: $processName" -ForegroundColor "Red"
    }

}

#You can use this function by calling it with the name of the process you want to bring to the foreground, like this:

#Set-ForegroundProcess -processName "FalconExporter"