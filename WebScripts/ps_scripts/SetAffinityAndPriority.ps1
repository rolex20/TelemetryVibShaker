#. "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts\Write-VerboseDebug.ps1" #Don't forget to include this file for independent testing

function SetAffinityAndPriority {
    param (
        [Parameter(Mandatory)] [bool]$SetEfficiencyAffinity,
        [Parameter(Mandatory)] [bool]$SetBackgroudPriority,
        [Parameter(Mandatory)] [bool]$SetIdlePriority
    )

    # Specify PROCESS_MODE_BACKGROUND_END using Win32 API function: SetPriorityClass
    # https://jakubjares.com/2015/03/06/lower-io-priority/
    Add-Type @"
    using System;
    using System.Runtime.InteropServices;

    namespace Utility
    {
      public class PriorityHelper
      {
        [DllImport("kernel32.dll", CharSet=CharSet.Auto, SetLastError=true)]
        static extern bool SetPriorityClass(IntPtr handle, PriorityClass priorityClass);

        public enum PriorityClass : uint
        {
            ABOVE_NORMAL_PRIORITY_CLASS = 0x8000,
            BELOW_NORMAL_PRIORITY_CLASS = 0x4000,
            HIGH_PRIORITY_CLASS = 0x80,
            IDLE_PRIORITY_CLASS = 0x40,
            NORMAL_PRIORITY_CLASS = 0x20,
            PROCESS_MODE_BACKGROUND_BEGIN = 0x100000,// 'Windows Vista/2008 and higher
            PROCESS_MODE_BACKGROUND_END = 0x200000,//   'Windows Vista/2008 and higher
            REALTIME_PRIORITY_CLASS = 0x100
        }

        public static int SetBackgroundIoPriority ()
        {
            IntPtr id = System.Diagnostics.Process.GetCurrentProcess().Handle;
            if (SetPriorityClass(id, PriorityClass.PROCESS_MODE_BACKGROUND_BEGIN)) return (int) id;
            return -1;
        }
      }
    }
"@

    if ($SetEfficiencyAffinity) {
        # Use Efficiency cores only, Idle and Background Processing mode
        $EfficiencyAffinity = 983040 # HyperThreading enabled for 12700K
        $EfficiencyAffinity = 268369920 # HyperThreading enabled for 14700K
        
        #[System.Diagnostics.Process]::GetCurrentProcess().ProcessorAffinity =  $EfficiencyAffinity
        try {
            [System.Diagnostics.Process]::GetCurrentProcess().ProcessorAffinity =  $EfficiencyAffinity
        } catch {
            Write-VerboseDebug -Timestamp (Get-Date) -Title "WARNING" -Message "Not an Intel 14700K " -ForegroundColor "Yellow"
        }
    }

    if ($SetBackgroudPriority) {
        $result = [Utility.PriorityHelper]::SetBackgroundIoPriority()
        if ($result -eq -1) {
              throw "Background IO priority could not be set or is already set"
        }
    }

    if ($SetIdlePriority) {
        [System.Diagnostics.Process]::GetCurrentProcess().PriorityClass = [System.Diagnostics.ProcessPriorityClass]::Idle # must come after SetBackgroundIoPriority
    }
}

#Example
#SetAffinityAndPriority -SetEfficiencyAffinity $true -SetBackgroudPriority $true -SetIdlePriority $true

