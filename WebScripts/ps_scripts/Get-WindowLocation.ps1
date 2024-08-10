#. "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WarThunderMissionGenerator\ps_scripts\Write-VerboseDebug.ps1" #Don't forget to include this file for independent testing

function Get-WindowLocation {
    param (
        [Parameter(Mandatory)] [string]$processName,
        [Parameter(Mandatory)] [int]$instance,
        [Parameter(Mandatory)] [string]$outFile
    )

    # Add user32.dll functions
    Add-Type @"
        using System;
        using System.Runtime.InteropServices;
        public class User32_GetLocation {
            [DllImport("user32.dll")]
            public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
            [DllImport("user32.dll")]
            public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();
            [StructLayout(LayoutKind.Sequential)]
            public struct RECT {
                public int Left;
                public int Top;
                public int Right;
                public int Bottom;
            }
        }
"@

    Write-VerboseDebug -Timestamp (Get-Date) -Title "GET-LOCATION" -Message $processName
    # Get the process
    $process = Get-Process -Name $processName -ErrorAction SilentlyContinue
    if ($process.Count -ge 1) {
        $process = $process[$instance]        
        # Bring the process to the foreground
        try{
            $hwnd = $process.MainWindowHandle
            # Get the window rectangle
            $rect = New-Object User32_GetLocation+RECT
            $r = [User32_GetLocation]::GetWindowRect($hWnd, [ref]$rect)
            
            # Print the coordinates
            $x = $rect.Left
            $y = $rect.Top

            # Write the file
            Write-Host $outFile
            Set-Content -Path $outFile -Value "$x`n$y"
        } catch {
            Write-VerboseDebug -Timestamp (Get-Date) -Title "GET-LOCATION-ERROR" -Message "SetWindowPos() failed for $processName" -ForegroundColor "Red"
        }

    } else {
        Write-VerboseDebug -Timestamp (Get-Date) -Title "GET-LOCATION-ERROR" -Message "Process not found: $processName" -ForegroundColor "Red"
    }

}

# Example usage
#Get-WindowLocation -processName "WarThunderExporter" -instance 0 -outfile "command.out"

#Get-Content "command.out"