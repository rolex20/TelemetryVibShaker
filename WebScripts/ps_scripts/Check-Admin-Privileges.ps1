Function Check-Admin-Privileges()
{
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
