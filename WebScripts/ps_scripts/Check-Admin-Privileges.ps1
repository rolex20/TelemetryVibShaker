#https://www.sharepointdiary.com/2015/01/run-powershell-script-as-administrator-automatically.html#:~:text=Enable%20%22Run%20as%20Administrator%22%20and%20click%20on%20%22OK%22,select%20the%20%22Run%20With%20Highest%20Privileges%22%20check%20box.
Function TryRunAsAdministrator()
{
  #Get current user context
  $CurrentUser = New-Object Security.Principal.WindowsPrincipal $([Security.Principal.WindowsIdentity]::GetCurrent())
  
  #Check user is running the script is member of Administrator Group
  if($CurrentUser.IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator))
  {
       Write-host "Script is running with Administrator privileges: [OK]"
  }
  else
    {
       #Create a new Elevated process to Start PowerShell
       $ElevatedProcess = New-Object System.Diagnostics.ProcessStartInfo "PowerShell";
 
       # Specify the current script path and name as a parameter
       $ElevatedProcess.Arguments = "& '" + $script:MyInvocation.MyCommand.Path + "'"
 
       #Set the Process to elevated
       $ElevatedProcess.Verb = "runas"
 
       #Start the new elevated process
       [System.Diagnostics.Process]::Start($ElevatedProcess)
 
       #Exit from the current, unelevated, process
       Exit
 
    }
}

Function Check-Admin-Privileges()
{
  TryRunAsAdministrator
  
  #Get current user context
  $CurrentUser = New-Object Security.Principal.WindowsPrincipal $([Security.Principal.WindowsIdentity]::GetCurrent())
  
  #Check user is running the script is member of Administrator Group
  if($CurrentUser.IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator))
  {
       return $true
  }
  else
    {
       Write-Host "This script requires elevated privileges.  Start it as an administrator"
       Start-Sleep -Seconds 20
       #Exit from the current, unelevated, process
       Exit
 
    }
}
