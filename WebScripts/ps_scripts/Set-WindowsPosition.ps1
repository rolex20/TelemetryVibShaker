#. "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts\Write-VerboseDebug.ps1" #Don't forget to include this file for independent testing

function Set-WindowPosition {
    param (
        [Parameter(Mandatory)] [string]$processName,
        [Parameter(Mandatory)] [int]$instance,
        [Parameter(Mandatory)] [int]$x,
        [Parameter(Mandatory)] [int]$y
    )

    $SWP_NOSIZE = 0x1
    $SWP_NOZORDER = 0x4

    Add-Type @"
        using System;
        using System.Runtime.InteropServices;
        public class User32_WindowsPosition {
            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);


            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

            public struct RECT {
                public int Left;
                public int Top;
                public int Right;
                public int Bottom;
            }
        }
"@

    Write-VerboseDebug -Timestamp (Get-Date) -Title "CHANGE_LOCATION" -Message $processName
    # Get the process
    $process = Get-Process -Name $processName -ErrorAction SilentlyContinue
    if ($process.Count -ge 1) {
        $process = $process[$instance]        
        # Bring the process to the foreground
        try{
            $hwnd = $process.MainWindowHandle
            $result = [User32_WindowsPosition]::SetWindowPos($hWnd, [IntPtr]::Zero, $x, $y, 0, 0, $SWP_NOSIZE -bor $SWP_NOZORDER)
        } catch {
            Write-VerboseDebug -Timestamp (Get-Date) -Title "CHANGE_LOCATION-ERROR" -Message "SetWindowPos() failed for $processName" -ForegroundColor "Red"
        }

    } else {
        Write-VerboseDebug -Timestamp (Get-Date) -Title "CHANGE_LOCATION-ERROR" -Message "Process not found: $processName" -ForegroundColor "Red"
    }

}

#You can use this function by calling it with the process name and the new X, Y coordinates. For example:

#Set-WindowPosition -processName "FalconExporter" -instance 0 -x 100 -y 200
#Set-WindowPosition -processName "WarThunderExporter" -instance 0 -x 100 -y 200
