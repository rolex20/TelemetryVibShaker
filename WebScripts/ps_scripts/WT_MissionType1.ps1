# Generate mission type 1 for War Thunder (Type 1 Dogfight with previewer)

. "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts\Include-Script.ps1"
$search_paths = @("C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts", "C:\MyPrograms\My Apps\TelemetryVibShaker\WebScripts\ps_scripts")
$include_file = Include-Script -FileName "Write-VerboseDebug.ps1" -Directories $search_paths
. $include_file

# create a sound player
$sound = New-Object System.Media.SoundPlayer;


function Play-Sound($player, $filename, $now) {
	$player.SoundLocation="C:\Windows\Media\Windows Notify.wav"	
	if ($now) 
	{
		$player.Play()	
	}
}



function Generate_WT_Mission_Type1() {
	$DownloadUrl1 = "http://localhost/warthunder/dogfight_setup_1.php"
	$DownloadPath1 = "C:\MyPrograms\Steam\steamapps\common\War Thunder\UserMissions\AutoDogfight_setup1.blk"
	$DownloadUrl2 = "http://localhost/warthunder/viewer_setup_1.php"
	$DownloadPath2 = "C:\MyPrograms\Steam\steamapps\common\War Thunder\UserMissions\AutoViewer_setup1.blk"

	Play-Sound $sound "C:\Windows\Media\Windows Notify.wav" $true
	
	try {
		$randomNumber = Get-Random -Minimum 1000 -Maximum 10000
		$url = $DownloadUrl1 + "?rnd=$randomNumber"
		Invoke-WebRequest -Uri $url -OutFile $DownloadPath1 -ErrorAction Stop
		$url = $DownloadUrl2 + "?rnd=$randomNumber"
		Invoke-WebRequest -Uri $url -OutFile $DownloadPath2 -ErrorAction Stop
		
		} catch 
		{
			Play-Sound $sound "C:\Windows\Media\Windows Critical Stop.wav" $true
		}
}

# notify we are about to start and force the player to be ready
#Play-Sound $sound "C:\Windows\Media\Windows Logon.wav" $true

# use a try...finally construct to release the
# filesystemwatcher once the loop is aborted
# by pressing CTRL+C
