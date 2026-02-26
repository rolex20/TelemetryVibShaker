# Generate mission type 1 for War Thunder (Type 1 Dogfight with previewer)

$sound = New-Object System.Media.SoundPlayer

function Play-Sound($player, $filename, $now) {
    $player.SoundLocation = 'C:\Windows\Media\Windows Notify.wav'
    if ($now) { $player.Play() }
}

function Generate_WT_Mission_Type1 {
    param([hashtable]$Config)

    $effectiveConfig = $Config
    if (-not $effectiveConfig) {
        $webScriptsRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
        $effectiveConfig = Get-TvsConfig -WebScriptsRoot $webScriptsRoot
    }

    $DownloadUrl1 = 'http://localhost/warthunder/dogfight_setup_1.php'
    $DownloadUrl2 = 'http://localhost/warthunder/viewer_setup_1.php'
    $userMissionsDir = $effectiveConfig.paths.wtUserMissionsDir
    if (-not (Test-Path $userMissionsDir)) { New-Item -ItemType Directory -Force -Path $userMissionsDir | Out-Null }
    $DownloadPath1 = Join-Path $userMissionsDir 'AutoDogfight_setup1.blk'
    $DownloadPath2 = Join-Path $userMissionsDir 'AutoViewer_setup1.blk'

    Play-Sound $sound 'C:\Windows\Media\Windows Notify.wav' $true

    try {
        $randomNumber = Get-Random -Minimum 1000 -Maximum 10000
        Invoke-WebRequest -Uri ($DownloadUrl1 + "?rnd=$randomNumber") -OutFile $DownloadPath1 -ErrorAction Stop
        Invoke-WebRequest -Uri ($DownloadUrl2 + "?rnd=$randomNumber") -OutFile $DownloadPath2 -ErrorAction Stop
    } catch {
        Play-Sound $sound 'C:\Windows\Media\Windows Critical Stop.wav' $true
    }
}
