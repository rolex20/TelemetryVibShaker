# Generate mission type 1 for War Thunder (Type 1 Dogfight with previewer)

. "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WebScripts\ps_scripts\Include-Script.ps1"
$search_paths = @("C:\MyPrograms\MyApps\TelemetryVibShaker\WebScripts", "C:\Users\ralch\source\repos\rolex20\TelemetryVibShaker\WebScripts\ps_scripts")
$include_file = Include-Script -FileName "Write-VerboseDebug.ps1" -Directories $search_paths
. $include_file

function Generate_WT_Mission_Type1() {
    $DownloadUrl1 = "http://localhost/warthunder/dogfight_setup_1.php" # This is the actual mission
    $DownloadPath1 = "C:\MyPrograms\Steam\steamapps\common\War Thunder\UserMissions\AutoDogfight_setup1.blk"
    $DownloadUrl2 = "http://localhost/warthunder/viewer_setup_1.php" # This is a pre-viewer for the AI planes to see if they have the right armament/ordinance
    $DownloadPath2 = "C:\MyPrograms\Steam\steamapps\common\War Thunder\UserMissions\AutoViewer_setup1.blk"


    # create a sound player
    $sound = New-Object System.Media.SoundPlayer
    $sound.SoundLocation="C:\Windows\Media\Windows Notify.wav"
	
    try {
	    Invoke-WebRequest -Uri $DownloadUrl1 -OutFile $DownloadPath1 -ErrorAction Stop
	    Invoke-WebRequest -Uri $DownloadUrl2 -OutFile $DownloadPath2 -ErrorAction Stop
		
	    } 
    catch {
            Write-VerboseDebug -Timestamp (Get-Date) -Title "WAR THUNDER ERROR" -Message "Invoke-WebRequest" -ForegroundColor "Red"
		    $sound.SoundLocation = "C:\Windows\Media\Windows Critical Stop.wav"
        
        }

    $sound.Play()
}