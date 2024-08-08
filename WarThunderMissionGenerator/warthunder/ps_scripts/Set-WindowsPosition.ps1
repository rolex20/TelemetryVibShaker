function Set-WindowPosition {
    param (
        [string]$processName,
        [int]$x,
        [int]$y
    )

    Add-Type @"
        using System;
        using System.Runtime.InteropServices;
        public class User32 {
            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

            public struct RECT {
                public int Left;
                public int Top;
                public int Right;
                public int Bottom;
            }
        }
"@

    $process = Get-Process -Name $processName -ErrorAction Stop   
    $hWnd = $process.MainWindowHandle

    if ($hWnd -eq [IntPtr]::Zero) {
        Write-Error "Window not found for process $processName"
        return
    }

    $rect = New-Object User32+RECT
    [User32]::GetWindowRect($hWnd, [ref]$rect) | Out-Null

    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top

    [User32]::MoveWindow($hWnd, $x, $y, $width, $height, $true) | Out-Null
    Write-Output "Window position for process $processName set to X: $x, Y: $y"
}

#You can use this function by calling it with the process name and the new X, Y coordinates. For example:

Set-WindowPosition -processName "FalconExporter" -x 100 -y 200