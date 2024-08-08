function Set-ForegroundProcess {
    param (
        [string]$processName
    )

    # Add necessary assemblies
    Add-Type @"
        using System;
        using System.Runtime.InteropServices;
        public class User32 {
            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetForegroundWindow(IntPtr hWnd);
        }
"@

    # Get the process
    $process = Get-Process -Name $processName -ErrorAction SilentlyContinue

    if ($process) {
        # Bring the process to the foreground
        $hwnd = $process.MainWindowHandle
        [User32]::SetForegroundWindow($hwnd)
        Write-Output "Process '$processName' is now in the foreground."
    } else {
        Write-Output "Process '$processName' not found."
    }
}

#You can use this function by calling it with the name of the process you want to bring to the foreground, like this:

Set-ForegroundProcess -processName "FalconExporter"