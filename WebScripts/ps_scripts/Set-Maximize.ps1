#. "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts\Write-VerboseDebug.ps1" #Don't forget to include this file

function Set-Maximize {
    param (
        [Parameter(Mandatory)] [string]$processName, 
        [Parameter(Mandatory)] [int]$instance
    )

    Write-VerboseDebug -Timestamp (Get-Date) -Title "MAXIMIZE" -Message $processName


    # Add necessary assemblies
    Add-Type @"
        using System;
        using System.Runtime.InteropServices;
        public class User32 {
            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
            [DllImport("user32.dll")]
            public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();
            [DllImport("user32.dll")]
            public static extern bool SetForegroundWindow(IntPtr hWnd);
        }
"@

    # Constants for ShowWindow
    $SW_MAXIMIZE = 3

    # Get the process
    $process = Get-Process -Name $processName -ErrorAction Stop

    # Maximize the window
    foreach ($proc in $process) {
        $hwnd = $proc.MainWindowHandle
        if ($hwnd -ne [IntPtr]::Zero) {
            [User32]::ShowWindow($hwnd, $SW_MAXIMIZE) | Out-Null
            [User32]::SetForegroundWindow($hwnd) | Out-Null
        }
    }
}

# Example usage
#Set-Maximize -processName "notepad" -instance 0

<#
function old_minimize {
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
#>

#You can use this function by calling it with the name of the process you want to bring to the foreground, like this:

#Set-Maximize -processName "FalconExporter" -instance 0