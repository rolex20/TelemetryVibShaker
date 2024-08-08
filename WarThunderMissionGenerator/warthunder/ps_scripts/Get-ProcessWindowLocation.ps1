function Get-ProcessWindowLocation {
    param (
        [string]$processName
    )

    # Add user32.dll functions
    Add-Type @"
        using System;
        using System.Runtime.InteropServices;
        public class User32 {
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

    # Get the process
    $process = Get-Process -Name $processName -ErrorAction SilentlyContinue
    if (-not $process) {
        Write-Host "Process not found."
        return
    }

    # Get the main window handle
    $hWnd = $process.MainWindowHandle
    if ($hWnd -eq [IntPtr]::Zero) {
        Write-Host "Main window not found."
        return
    }

    # Get the window rectangle
    $rect = New-Object User32+RECT
    [User32]::GetWindowRect($hWnd, [ref]$rect)

    # Calculate the X and Y coordinates
    $x = $rect.Left
    $y = $rect.Top

    # Print the coordinates
    Write-Host "X: $x, Y: $y"
}

# Example usage
Get-ProcessWindowLocation -processName "FalconExporter"
