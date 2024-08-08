function Close-ProcessNicely {
    param (
        [string]$processName
    )

    # Add user32.dll functions
    Add-Type @"
        using System;
        using System.Runtime.InteropServices;
        public class User32 {
            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        }
"@

    # Constants
    $WM_CLOSE = 0x0010

    # Get the main window handle of the process
    $process = Get-Process -Name $processName -ErrorAction SilentlyContinue
    if ($process) {
        $mainWindowHandle = $process.MainWindowHandle
        if ($mainWindowHandle -ne [IntPtr]::Zero) {
            # Send WM_CLOSE message
            $r = [User32]::PostMessage($mainWindowHandle, $WM_CLOSE, [IntPtr]::Zero, [IntPtr]::Zero)
            Write-Output "Sent WM_CLOSE message to $processName."
        } else {
            Write-Output "Could not find the main window handle for $processName."
        }
    } else {
        Write-Output "Process $processName not found."
    }
}

#You can use this function by passing the process name as an argument. For example:

Close-ProcessNicely -processName "FalconExporter"

#The most common way to end a program in PowerShell is by using the Stop-Process cmdlet. This cmdlet can terminate a process by its name or process ID (PID). Here’s a simple example of how to use it:
    # Terminate a process by its name
    Stop-Process -Name "FalconExporter"

    # Terminate a process by its PID
    Stop-Process -Id 1234