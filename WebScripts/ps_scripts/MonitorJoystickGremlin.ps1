# Description: Warns if Joystick Gremlin is using having memory leak issues
# Note: Before starting the main loop, we will use Efficient cores only, Idle and Background processing modes



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


$ProcessName = "joystick_gremlin"
$ShortName = "joy-gremlin"
Add-Type -AssemblyName System.Speech


$synth = New-Object -TypeName System.Speech.Synthesis.SpeechSynthesizer
$synth.Volume = 100

$global:waitTime = 60*3 # seconds

Write-Host "Running..."

# Use Efficiency cores only, Idle and Background Processing mode
$EfficiencyAffinity = 983040 # HyperThreading enabled for 12700K
$EfficiencyAffinity = 268369920 # HyperThreading enabled for 14700K
[System.Diagnostics.Process]::GetCurrentProcess().ProcessorAffinity =  $EfficiencyAffinity
$result = [Utility.PriorityHelper]::SetBackgroundIoPriority()
if ($result -eq -1) {
      throw "Background IO priority could not be set or is already set"
}
[System.Diagnostics.Process]::GetCurrentProcess().PriorityClass = [System.Diagnostics.ProcessPriorityClass]::Idle # must come after SetBackgroundIoPriority

# Start of Main-Loop
while ($true) {

    $M = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue

    if ( $M -EQ $null) {
            $w = "Error: $ShortName is not running."
            Write-Host ($d.ToShortDateString() + " " + $d.ToShortTimeString() + " - " + $w)
            $synth.Speak($w)
    } else {

            $c = ($M.PrivateMemorySize64 / 1Mb).ToInt32($null)
            $d = date
            $w = "$ShortName : $c megabytes."
            Write-Host ($d.ToShortDateString() + " " + $d.ToShortTimeString() + " - " + $w)
		
        if ($M.PrivateMemorySize64 -GT 110000000) {
			$w = "Warning: $ShortName is consuming too much memory: $c megabytes."
            $synth.Speak($w)
        } else {
				$i = $i + 1
				if ( $i % 2 -EQ 0 ) { $synth.Speak("J. Gremlin is O.K.") }
		}
    }

	#Check on PowerPlan
	if ( $p % 300 -EQ 0 ) { #ejecutar solo cada 5 minutos
		$output = (& powercfg.exe '/getactivescheme')
		if ($output -LIKE '*performance*') {
			$synth.Speak("Power Plan is O.K.") 	
		} else {			
			#Start-Process -FilePath "powercfg.exe" -ArgumentList "/SETACTIVE 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"
			$synth.Speak("Power Plan has been set to performance mode.")
		}
	}	
	$p = $p + $global:waitTime	

	#DONT:Since DCS-mt, skip DCS checks
	if ($false) {
		#Check on DCS Priority Class	
		$M = Get-Process -Name "DCS" -ErrorAction SilentlyContinue
		if ($M -NE $null -AND $M.PriorityClass -NE 'AboveNormal') {
			$synth.Speak(" WARNING: DCS Priority Class is not Above-Normal.") 
			$synth.Speak(" Priority Class currently is: " + $M.PriorityClass + ".  Trying to change..." ) 
			$M.PriorityClass = 'AboveNormal'	
		} elseif ($M -NE $null -AND $M.ProcessorAffinity -NE 65535) {
			$synth.Speak(" WARNING: DCS Affinity is not on Perfomance Cores only.   Trying to change.") 
			$M.ProcessorAffinity=65535
		} elseif ($M -EQ $null ) {
			$M = Get-Process -Name "aces" -ErrorAction SilentlyContinue
			if ($M -EQ $null ) { $M = Get-Process -Name "FlightSimulator" -ErrorAction SilentlyContinue }
			if ($M -EQ $null ) { 
				$synth.Speak(" The simulator is not running.") 
				#$synth.Speak(" Monitor Joystick is about to exit.")
				#break;
			}
		} else {
			$synth.Speak(" DCS is O.K.") 
		}
	} # end-if

    Start-Sleep -s $global:waitTime
	

} # end-while